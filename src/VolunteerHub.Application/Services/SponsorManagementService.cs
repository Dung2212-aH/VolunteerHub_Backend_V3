using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class SponsorManagementService : ISponsorManagementService
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminAuditService _adminAuditService;

    public SponsorManagementService(ISponsorRepository sponsorRepository, IUnitOfWork unitOfWork, IAdminAuditService adminAuditService)
    {
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _adminAuditService = adminAuditService;
    }

    public async Task<Result<List<SponsorProfileResponse>>> GetPendingSponsorProfilesAsync(CancellationToken cancellationToken = default)
        => Result.Success((await _sponsorRepository.GetPendingSponsorProfilesAsync(cancellationToken)).Select(p => new SponsorProfileResponse { Id = p.Id, UserId = p.UserId, CompanyName = p.CompanyName, Description = p.Description, LogoUrl = p.LogoUrl, WebsiteUrl = p.WebsiteUrl, Email = p.Email, Phone = p.Phone, Address = p.Address, TaxCode = p.TaxCode, ContactPersonName = p.ContactPersonName, ContactPersonEmail = p.ContactPersonEmail, ContactPersonPhone = p.ContactPersonPhone, ContactPersonRole = p.ContactPersonRole, Status = p.Status.ToString(), RejectionReason = p.RejectionReason }).ToList());

    public async Task<Result> ReviewSponsorProfileAsync(Guid sponsorProfileId, ApproveSponsorProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _sponsorRepository.GetSponsorProfileByIdAsync(sponsorProfileId, cancellationToken);
        if (profile == null) return Result.Failure(Error.NotFound);
        profile.Status = request.Approve ? SponsorProfileStatus.Approved : SponsorProfileStatus.Rejected;
        profile.RejectionReason = request.Approve ? null : request.Reason;
        _sponsorRepository.UpdateSponsorProfile(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _adminAuditService.LogAsync(Guid.Empty, request.Approve ? "SponsorProfile.Approved" : "SponsorProfile.Rejected", nameof(SponsorProfile), profile.Id, request.Reason ?? string.Empty, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<List<EventSponsorResponse>>> GetEventSponsorsAsync(Guid organizerUserId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var ev = await _sponsorRepository.GetEventByIdAsync(eventId, cancellationToken);
        if (ev == null || ev.OrganizerId != organizerUserId) return Result.Failure<List<EventSponsorResponse>>(Error.NotFound);
        return Result.Success((await _sponsorRepository.GetEventSponsorsByEventIdAsync(eventId, cancellationToken)).Select(i => new EventSponsorResponse { Id = i.Id, SponsorProfileId = i.SponsorProfileId, CompanyName = i.SponsorProfile.CompanyName, LogoUrl = i.SponsorProfile.LogoUrl, EventId = i.EventId, EventTitle = i.Event.Title, Status = i.Status.ToString(), IsPubliclyVisible = i.IsPubliclyVisible, SponsorshipType = i.SponsorshipType, ProposedValue = i.ProposedValue, Note = i.Note, RejectionReason = i.RejectionReason }).ToList());
    }

    public async Task<Result> ReviewEventSponsorAsync(Guid organizerUserId, Guid eventSponsorId, ApproveRejectEventSponsorRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _sponsorRepository.GetEventSponsorByIdAsync(eventSponsorId, cancellationToken);
        if (item == null || item.Event.OrganizerId != organizerUserId) return Result.Failure(Error.NotFound);
        item.Status = request.Approve ? EventSponsorStatus.Approved : EventSponsorStatus.Rejected;
        item.RejectionReason = request.Approve ? null : request.Reason;
        item.IsPubliclyVisible = request.Approve && request.IsPubliclyVisible;
        _sponsorRepository.UpdateEventSponsor(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RecordContributionAsync(Guid organizerUserId, RecordContributionRequest request, CancellationToken cancellationToken = default)
    {
        var eventSponsor = await _sponsorRepository.GetEventSponsorByIdAsync(request.EventSponsorId, cancellationToken);
        if (eventSponsor == null || eventSponsor.Event.OrganizerId != organizerUserId) return Result.Failure(Error.NotFound);
        if (eventSponsor.Status != EventSponsorStatus.Approved) return Result.Failure(new Error("Sponsor.InvalidSponsorStatus", "Only approved sponsors can have contributions recorded."));
        if (!Enum.TryParse<ContributionType>(request.Type, true, out var contributionType)) return Result.Failure(new Error("Sponsor.InvalidContributionType", "Contribution type must be Monetary or InKind."));
        _sponsorRepository.AddContribution(new SponsorContribution { EventSponsorId = eventSponsor.Id, SponsorProfileId = eventSponsor.SponsorProfileId, Type = contributionType, Value = request.Value, Description = request.Description, ContributedAt = request.ContributedAt ?? DateTime.UtcNow, ReceiptReference = request.ReceiptReference, Note = request.Note });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
