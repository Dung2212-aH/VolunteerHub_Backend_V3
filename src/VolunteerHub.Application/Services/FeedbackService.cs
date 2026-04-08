using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Rating;
using VolunteerHub.Domain.Entities;
using VolunteerHub.Domain.Enums;

namespace VolunteerHub.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IRatingRepository _ratingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationApprovalRepository _appRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IVolunteerProfileRepository _profileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FeedbackService(
        IRatingRepository ratingRepository,
        IEventRepository eventRepository,
        IApplicationApprovalRepository appRepository,
        IAttendanceRepository attendanceRepository,
        IVolunteerProfileRepository profileRepository,
        IUnitOfWork unitOfWork)
    {
        _ratingRepository = ratingRepository;
        _eventRepository = eventRepository;
        _appRepository = appRepository;
        _attendanceRepository = attendanceRepository;
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> SubmitReportAsync(Guid reporterUserId, CreateFeedbackReportRequest request, CancellationToken cancellationToken = default)
    {
        // Reason is required
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(new Error("Feedback.ReasonRequired", "A reason is required for the report."));

        if (request.Reason.Length > 500)
            return Result.Failure(new Error("Feedback.ReasonTooLong", "Reason must not exceed 500 characters."));

        if (request.Description != null && request.Description.Length > 4000)
            return Result.Failure(new Error("Feedback.DescriptionTooLong", "Description must not exceed 4000 characters."));

        var rating = await _ratingRepository.GetByIdAsync(request.RatingId, cancellationToken);
        if (rating == null)
            return Result.Failure(Error.NotFound);

        if (rating.EventId != request.EventId)
            return Result.Failure(new Error("Feedback.InvalidContext", "The report must match the rating event context."));

        if (rating.FromUserId != reporterUserId && rating.ToUserId != reporterUserId)
            return Result.Failure(new Error("Feedback.InvalidReporter", "Only users involved in the rating can submit a report."));

        var ev = await _eventRepository.GetDetailsByIdAsync(request.EventId, cancellationToken);
        if (ev == null)
            return Result.Failure(Error.NotFound);

        var reporterRole = rating.FromUserId == reporterUserId ? rating.FromRole : rating.ToRole;
        if (reporterRole == RatingRole.Volunteer)
        {
            var reporterProfile = await _profileRepository.GetByUserIdWithDetailsAsync(reporterUserId, cancellationToken);
            if (reporterProfile == null
                || !await _appRepository.IsApprovedAsync(request.EventId, reporterProfile.Id, cancellationToken)
                || !await _attendanceRepository.HasApprovedAttendanceAsync(request.EventId, reporterProfile.Id, cancellationToken))
            {
                return Result.Failure(new Error("Feedback.InvalidReporter", "Reporter must be a valid participant for this event."));
            }
        }
        else if (reporterRole == RatingRole.Organizer && ev.OrganizerId != reporterUserId)
        {
            return Result.Failure(new Error("Feedback.InvalidReporter", "Reporter must be the organizer for this event."));
        }

        var report = new FeedbackReport
        {
            RatingId = rating.Id,
            EventId = request.EventId,
            ReporterUserId = reporterUserId,
            TargetUserId = rating.FromUserId == reporterUserId ? rating.ToUserId : rating.FromUserId,
            Reason = request.Reason,
            Description = request.Description
        };

        _ratingRepository.AddFeedbackReport(report);

        if (rating.Status == RatingStatus.Active)
        {
            rating.Status = RatingStatus.UnderReview;
            _ratingRepository.Update(rating);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<List<FeedbackReportResponse>>> GetMyReportsAsync(Guid reporterUserId, CancellationToken cancellationToken = default)
    {
        var reports = await _ratingRepository.GetFeedbackByReporterAsync(reporterUserId, cancellationToken);
        var response = reports.Select(r => new FeedbackReportResponse
        {
            Id = r.Id,
            RatingId = r.RatingId,
            EventId = r.EventId,
            ReporterUserId = r.ReporterUserId,
            TargetUserId = r.TargetUserId,
            Reason = r.Reason,
            Description = r.Description,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt
        }).ToList();

        return Result.Success(response);
    }
}
