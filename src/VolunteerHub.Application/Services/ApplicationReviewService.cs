using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class ApplicationReviewService : IApplicationReviewService
{
    private readonly IApplicationApprovalRepository _appRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IVolunteerProfileRepository _profileRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public ApplicationReviewService(IApplicationApprovalRepository appRepository, IEventRepository eventRepository, IVolunteerProfileRepository profileRepository, INotificationService notificationService, IUnitOfWork unitOfWork)
    {
        _appRepository = appRepository;
        _eventRepository = eventRepository;
        _profileRepository = profileRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    private async Task<Result<(EventApplication App, Event Event)>> GetValidApplicationAsync(Guid organizerId, Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await _appRepository.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (app == null) return Result.Failure<(EventApplication, Event)>(Error.NotFound);
        var ev = await _eventRepository.GetDetailsByIdAsync(app.EventId, cancellationToken);
        if (ev == null || ev.OrganizerId != organizerId) return Result.Failure<(EventApplication, Event)>(Error.Unauthorized);
        return Result.Success((app, ev));
    }

    public Task<Result> ApproveAsync(Guid organizerId, Guid applicationId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(organizerId, applicationId, ApplicationStatus.Approved, null, cancellationToken);

    public Task<Result> RejectAsync(Guid organizerId, Guid applicationId, ReviewApplicationRequest request, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(organizerId, applicationId, ApplicationStatus.Rejected, request.Reason, cancellationToken);

    public Task<Result> WaitlistAsync(Guid organizerId, Guid applicationId, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(organizerId, applicationId, ApplicationStatus.Waitlisted, null, cancellationToken);

    private async Task<Result> ChangeStatusAsync(Guid organizerId, Guid applicationId, ApplicationStatus status, string? reason, CancellationToken cancellationToken)
    {
        var result = await GetValidApplicationAsync(organizerId, applicationId, cancellationToken);
        if (!result.IsSuccess) return Result.Failure(result.Error);
        var app = result.Value.App;
        var ev = result.Value.Event;

        if (status == ApplicationStatus.Approved)
        {
            var approvedCount = await _appRepository.GetApprovedApplicationsCountAsync(app.EventId, cancellationToken);
            if (approvedCount >= ev.Capacity) return Result.Failure(new Error("Application.CapacityExceeded", "The event capacity has been reached."));
        }

        app.Status = status;
        app.ReviewedAt = DateTime.UtcNow;
        app.ReviewedByUserId = organizerId;
        app.ReviewNote = string.IsNullOrWhiteSpace(reason) ? null : reason;
        app.RejectionReason = status == ApplicationStatus.Rejected ? reason : null;
        _appRepository.UpdateApplication(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = await _profileRepository.GetByIdWithDetailsAsync(app.VolunteerProfileId, cancellationToken);
        if (profile != null)
        {
            if (status == ApplicationStatus.Approved)
                await _notificationService.NotifyApplicationApprovedAsync(profile.UserId, ev.Title, app.Id, cancellationToken);
            else if (status == ApplicationStatus.Rejected)
                await _notificationService.NotifyApplicationRejectedAsync(profile.UserId, ev.Title, app.Id, reason, cancellationToken);
        }
        return Result.Success();
    }

    public async Task<Result<List<ApplicantSummaryResponse>>> GetEventApplicationsAsync(Guid organizerId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var ev = await _eventRepository.GetDetailsByIdAsync(eventId, cancellationToken);
        if (ev == null || ev.OrganizerId != organizerId) return Result.Failure<List<ApplicantSummaryResponse>>(Error.Unauthorized);
        var apps = await _appRepository.GetApplicationsByEventAsync(eventId, cancellationToken);
        return Result.Success(apps.Select(Map).ToList());
    }

    public async Task<Result<ApplicantSummaryResponse>> GetApplicationDetailsAsync(Guid organizerId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var result = await GetValidApplicationAsync(organizerId, applicationId, cancellationToken);
        if (!result.IsSuccess) return Result.Failure<ApplicantSummaryResponse>(result.Error);
        return Result.Success(Map(result.Value.App));
    }

    private static ApplicantSummaryResponse Map(EventApplication app) => new()
    {
        Id = app.Id,
        EventId = app.EventId,
        VolunteerProfileId = app.VolunteerProfileId,
        Status = app.Status.ToString(),
        AppliedAt = app.AppliedAt,
        MotivationText = app.MotivationText,
        AvailabilityNote = app.AvailabilityNote,
        ReviewedAt = app.ReviewedAt,
        ReviewNote = app.ReviewNote,
        RejectionReason = app.RejectionReason,
        VolunteerFullName = app.VolunteerProfile?.FullName ?? string.Empty
    };
}
