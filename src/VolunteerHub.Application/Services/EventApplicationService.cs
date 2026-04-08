using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class EventApplicationService : IEventApplicationService
{
    private readonly IApplicationApprovalRepository _appRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IVolunteerProfileRepository _profileRepository;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public EventApplicationService(IApplicationApprovalRepository appRepository, IEventRepository eventRepository, IVolunteerProfileRepository profileRepository, INotificationService notificationService, IUnitOfWork unitOfWork)
    {
        _appRepository = appRepository;
        _eventRepository = eventRepository;
        _profileRepository = profileRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ApplyAsync(Guid volunteerProfileId, ApplyToEventRequest request, CancellationToken cancellationToken = default)
    {
        var ev = await _eventRepository.GetDetailsByIdAsync(request.EventId, cancellationToken);
        if (ev == null) return Result.Failure(Error.NotFound);

        if (ev.Status != EventStatus.Published)
            return Result.Failure(new Error("Application.EventNotOpen", "Applications are only allowed for published events."));

        if (DateTime.UtcNow >= ev.StartAt)
            return Result.Failure(new Error("Application.DeadlinePassed", "The registration deadline for this event has passed."));

        var activeCount = await _appRepository.GetActiveApplicationsCountAsync(request.EventId, cancellationToken);
        if (activeCount >= ev.Capacity)
            return Result.Failure(new Error("Application.CapacityExceeded", "The event is already at full capacity."));

        if (await _appRepository.HasActiveApplicationAsync(request.EventId, volunteerProfileId, cancellationToken))
            return Result.Failure(new Error("Application.Duplicate", "You already have an active application for this event."));

        var application = new EventApplication
        {
            EventId = request.EventId,
            VolunteerProfileId = volunteerProfileId,
            MotivationText = request.MotivationText,
            AvailabilityNote = request.AvailabilityNote,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };
        _appRepository.AddApplication(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = await _profileRepository.GetByIdWithDetailsAsync(volunteerProfileId, cancellationToken);
        if (profile != null)
            await _notificationService.NotifyApplicationSubmittedAsync(profile.UserId, ev.Title, application.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> WithdrawAsync(Guid volunteerProfileId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _appRepository.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (application == null || application.VolunteerProfileId != volunteerProfileId) return Result.Failure(Error.NotFound);
        if (application.Status != ApplicationStatus.Pending && application.Status != ApplicationStatus.UnderReview)
            return Result.Failure(new Error("Application.CannotWithdraw", "This application can no longer be withdrawn."));
        application.Status = ApplicationStatus.Cancelled;
        application.WithdrawnAt = DateTime.UtcNow;
        application.ReviewNote = "Withdrawn by volunteer.";
        _appRepository.UpdateApplication(application);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ApplicationResponse>> GetApplicationAsync(Guid volunteerProfileId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var app = await _appRepository.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (app == null || app.VolunteerProfileId != volunteerProfileId) return Result.Failure<ApplicationResponse>(Error.NotFound);
        return Result.Success(Map(app));
    }

    public async Task<Result<List<ApplicationResponse>>> GetMyApplicationsAsync(Guid volunteerProfileId, CancellationToken cancellationToken = default)
    {
        var apps = await _appRepository.GetMyApplicationsAsync(volunteerProfileId, cancellationToken);
        return Result.Success(apps.Select(Map).ToList());
    }

    private static ApplicationResponse Map(EventApplication app) => new()
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
        RejectionReason = app.RejectionReason
    };
}
