using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class SponsorProfileService : ISponsorProfileService
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SponsorProfileService(ISponsorRepository sponsorRepository, IUnitOfWork unitOfWork)
    {
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SponsorProfileResponse>> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _sponsorRepository.GetSponsorProfileByUserIdAsync(userId, cancellationToken);
        if (profile == null) return Result.Failure<SponsorProfileResponse>(Error.NotFound);
        return Result.Success(MapProfile(profile));
    }

    public async Task<Result> CreateProfileAsync(Guid userId, CreateSponsorProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (await _sponsorRepository.SponsorProfileExistsForUserAsync(userId, cancellationToken))
            return Result.Failure(new Error("Sponsor.AlreadyExists", "Sponsor profile already exists for this user."));
        var profile = new SponsorProfile
        {
            UserId = userId,
            CompanyName = request.CompanyName,
            Description = request.Description,
            LogoUrl = request.LogoUrl ?? string.Empty,
            WebsiteUrl = request.WebsiteUrl ?? string.Empty,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            TaxCode = request.TaxCode,
            ContactPersonName = request.ContactPersonName,
            ContactPersonEmail = request.ContactPersonEmail,
            ContactPersonPhone = request.ContactPersonPhone,
            ContactPersonRole = request.ContactPersonRole,
            Status = SponsorProfileStatus.PendingApproval
        };
        _sponsorRepository.AddSponsorProfile(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, UpdateSponsorProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _sponsorRepository.GetSponsorProfileByUserIdAsync(userId, cancellationToken);
        if (profile == null) return Result.Failure(Error.NotFound);
        if (profile.Status == SponsorProfileStatus.Suspended) return Result.Failure(new Error("Sponsor.Suspended", "Suspended sponsor profiles cannot be updated."));
        profile.CompanyName = request.CompanyName;
        profile.Description = request.Description;
        profile.LogoUrl = request.LogoUrl ?? string.Empty;
        profile.WebsiteUrl = request.WebsiteUrl ?? string.Empty;
        profile.Email = request.Email;
        profile.Phone = request.Phone;
        profile.Address = request.Address;
        profile.TaxCode = request.TaxCode;
        profile.ContactPersonName = request.ContactPersonName;
        profile.ContactPersonEmail = request.ContactPersonEmail;
        profile.ContactPersonPhone = request.ContactPersonPhone;
        profile.ContactPersonRole = request.ContactPersonRole;
        if (profile.Status == SponsorProfileStatus.Rejected) { profile.Status = SponsorProfileStatus.PendingApproval; profile.RejectionReason = null; }
        _sponsorRepository.UpdateSponsorProfile(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RequestSponsorEventAsync(Guid userId, SponsorEventRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _sponsorRepository.GetSponsorProfileByUserIdAsync(userId, cancellationToken);
        if (profile == null) return Result.Failure(Error.NotFound);
        if (profile.Status != SponsorProfileStatus.Approved) return Result.Failure(new Error("Sponsor.ProfileNotApproved", "Only approved sponsor profiles can request event sponsorship."));
        var ev = await _sponsorRepository.GetEventByIdAsync(request.EventId, cancellationToken);
        if (ev == null) return Result.Failure(Error.NotFound);
        if (await _sponsorRepository.HasActiveEventSponsorAsync(profile.Id, request.EventId, cancellationToken)) return Result.Failure(new Error("Sponsor.DuplicateRequest", "This sponsor already has an active sponsorship request for the event."));
        _sponsorRepository.AddEventSponsor(new EventSponsor { SponsorProfileId = profile.Id, EventId = request.EventId, Status = EventSponsorStatus.Pending, IsPubliclyVisible = false, SponsorshipType = request.SponsorshipType, ProposedValue = request.ProposedValue, Note = request.Note });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<List<EventSponsorResponse>>> GetMyEventSponsorsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _sponsorRepository.GetSponsorProfileByUserIdAsync(userId, cancellationToken);
        if (profile == null) return Result.Failure<List<EventSponsorResponse>>(Error.NotFound);
        return Result.Success((await _sponsorRepository.GetEventSponsorsBySponsorProfileIdAsync(profile.Id, cancellationToken)).Select(MapEventSponsor).ToList());
    }

    private static SponsorProfileResponse MapProfile(SponsorProfile profile) => new()
    {
        Id = profile.Id, UserId = profile.UserId, CompanyName = profile.CompanyName, Description = profile.Description, LogoUrl = profile.LogoUrl, WebsiteUrl = profile.WebsiteUrl, Email = profile.Email, Phone = profile.Phone, Address = profile.Address, TaxCode = profile.TaxCode,
        ContactPersonName = profile.ContactPersonName, ContactPersonEmail = profile.ContactPersonEmail, ContactPersonPhone = profile.ContactPersonPhone, ContactPersonRole = profile.ContactPersonRole, Status = profile.Status.ToString(), RejectionReason = profile.RejectionReason
    };
    private static EventSponsorResponse MapEventSponsor(EventSponsor item) => new() { Id = item.Id, SponsorProfileId = item.SponsorProfileId, CompanyName = item.SponsorProfile.CompanyName, LogoUrl = item.SponsorProfile.LogoUrl, EventId = item.EventId, EventTitle = item.Event.Title, Status = item.Status.ToString(), IsPubliclyVisible = item.IsPubliclyVisible, SponsorshipType = item.SponsorshipType, ProposedValue = item.ProposedValue, Note = item.Note, RejectionReason = item.RejectionReason };
}
