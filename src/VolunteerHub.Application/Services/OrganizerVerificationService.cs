using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class OrganizerVerificationService : IOrganizerVerificationService
{
    private readonly IOrganizerRepository _organizerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminAuditService _adminAuditService;

    public OrganizerVerificationService(IOrganizerRepository organizerRepository, IUnitOfWork unitOfWork, IAdminAuditService adminAuditService)
    {
        _organizerRepository = organizerRepository;
        _unitOfWork = unitOfWork;
        _adminAuditService = adminAuditService;
    }

    public async Task<bool> IsOrganizerVerifiedAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        var profile = await _organizerRepository.GetByUserIdAsync(organizerId, cancellationToken);
        return profile != null && profile.VerificationStatus == OrganizerVerificationStatus.Approved;
    }

    public async Task<Result<List<OrganizerProfileResponse>>> GetPendingVerificationsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _organizerRepository.GetPendingProfilesAsync(cancellationToken);
        return Result.Success(pending.Select(p => new OrganizerProfileResponse
        {
            Id = p.Id,
            UserId = p.UserId,
            OrganizationName = p.OrganizationName,
            OrganizationType = p.OrganizationType,
            RegistrationNumber = p.RegistrationNumber,
            TaxCode = p.TaxCode,
            Description = p.Description,
            Mission = p.Mission,
            WebsiteUrl = p.WebsiteUrl,
            LogoUrl = p.LogoUrl,
            Email = p.Email,
            Phone = p.Phone,
            Address = p.Address,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            LegalDocumentUrl = p.LegalDocumentUrl,
            VerificationStatus = p.VerificationStatus.ToString(),
            VerifiedAt = p.VerifiedAt,
            RejectionReason = p.RejectionReason
        }).ToList());
    }

    public Task<Result> ApproveOrganizerAsync(Guid adminId, Guid profileId, ReviewOrganizerVerificationRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adminId, profileId, OrganizerVerificationStatus.Approved, request.Comment, cancellationToken);
    public Task<Result> RejectOrganizerAsync(Guid adminId, Guid profileId, ReviewOrganizerVerificationRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adminId, profileId, OrganizerVerificationStatus.Rejected, request.Comment, cancellationToken);
    public Task<Result> SuspendOrganizerAsync(Guid adminId, Guid profileId, ReviewOrganizerVerificationRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(adminId, profileId, OrganizerVerificationStatus.Suspended, request.Comment, cancellationToken);

    private async Task<Result> ChangeStatusAsync(Guid adminId, Guid profileId, OrganizerVerificationStatus status, string comment, CancellationToken cancellationToken)
    {
        var profile = await _organizerRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile == null) return Result.Failure(Error.NotFound);
        profile.VerificationStatus = status;
        profile.VerifiedAt = status == OrganizerVerificationStatus.Approved ? DateTime.UtcNow : profile.VerifiedAt;
        profile.RejectionReason = status == OrganizerVerificationStatus.Rejected || status == OrganizerVerificationStatus.Suspended ? comment : null;
        _organizerRepository.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _adminAuditService.LogAsync(adminId, $"Organizer.{status}", nameof(OrganizerProfile), profile.Id, comment, cancellationToken);
        return Result.Success();
    }
}
