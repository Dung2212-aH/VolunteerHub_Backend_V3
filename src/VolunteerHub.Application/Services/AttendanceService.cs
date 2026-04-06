using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Application.Helpers;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationApprovalRepository _appRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const double MaxGpsDistanceKm = 0.5;

    public AttendanceService(IAttendanceRepository attendanceRepository, IEventRepository eventRepository, IApplicationApprovalRepository appRepository, IUnitOfWork unitOfWork)
    {
        _attendanceRepository = attendanceRepository;
        _eventRepository = eventRepository;
        _appRepository = appRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> CheckInAsync(Guid volunteerProfileId, CheckInRequest request, CancellationToken cancellationToken = default)
    {
        var isApproved = await _appRepository.IsApprovedAsync(request.EventId, volunteerProfileId, cancellationToken);
        if (!isApproved) return Result.Failure(new Error("Attendance.NotApproved", "Volunteer must have an approved application for this event."));

        var ev = await _eventRepository.GetDetailsByIdAsync(request.EventId, cancellationToken);
        if (ev == null) return Result.Failure(Error.NotFound);
        var now = DateTime.UtcNow;
        if (now < ev.StartAt.AddHours(-1) || now > ev.EndAt) return Result.Failure(new Error("Attendance.InvalidTimeWindow", "Check-in is not currently open for this event."));

        if (!Enum.TryParse<CheckInMethod>(request.Method, true, out var methodType)) methodType = CheckInMethod.None;
        if (methodType == CheckInMethod.GPS && request.Latitude.HasValue && request.Longitude.HasValue && ev.Latitude.HasValue && ev.Longitude.HasValue)
        {
            var dist = LocationHelper.CalculateDistanceKm(ev.Latitude.Value, ev.Longitude.Value, request.Latitude.Value, request.Longitude.Value);
            if (dist > MaxGpsDistanceKm) return Result.Failure(new Error("Attendance.OutOfRange", "You are too far from the event location."));
        }

        var record = await _attendanceRepository.GetRecordAsync(request.EventId, volunteerProfileId, cancellationToken);
        if (record != null && record.CheckInAt.HasValue) return Result.Failure(new Error("Attendance.Duplicate", "You have already checked in."));
        record ??= new AttendanceRecord { EventId = request.EventId, VolunteerProfileId = volunteerProfileId };
        record.CheckInAt = now;
        record.CheckInMethod = methodType;
        record.CheckInLatitude = request.Latitude;
        record.CheckInLongitude = request.Longitude;
        record.Status = AttendanceStatus.CheckedIn;
        if (record.Id == Guid.Empty) _attendanceRepository.AddAttendanceRecord(record); else _attendanceRepository.UpdateAttendanceRecord(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> CheckOutAsync(Guid volunteerProfileId, CheckOutRequest request, CancellationToken cancellationToken = default)
    {
        var ev = await _eventRepository.GetDetailsByIdAsync(request.EventId, cancellationToken);
        if (ev == null) return Result.Failure(Error.NotFound);
        var record = await _attendanceRepository.GetRecordAsync(request.EventId, volunteerProfileId, cancellationToken);
        if (record == null || !record.CheckInAt.HasValue) return Result.Failure(new Error("Attendance.NoCheckIn", "You must check in first."));
        if (!Enum.TryParse<CheckInMethod>(request.Method, true, out var methodType)) methodType = CheckInMethod.None;
        var now = DateTime.UtcNow;
        record.CheckOutAt = now;
        record.CheckOutMethod = methodType;
        record.CheckOutLatitude = request.Latitude;
        record.CheckOutLongitude = request.Longitude;
        var duration = now - record.CheckInAt.Value;
        record.ApprovedHours = Math.Round(Math.Max(duration.TotalHours, 0), 2);
        record.Status = AttendanceStatus.CheckedOut;
        _attendanceRepository.UpdateAttendanceRecord(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ManualOverrideAsync(Guid organizerId, Guid eventId, ManualOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var ev = await _eventRepository.GetDetailsByIdAsync(eventId, cancellationToken);
        if (ev == null || ev.OrganizerId != organizerId) return Result.Failure(Error.NotFound);
        var record = await _attendanceRepository.GetRecordAsync(eventId, request.VolunteerProfileId, cancellationToken);
        if (record == null)
        {
            record = new AttendanceRecord { EventId = eventId, VolunteerProfileId = request.VolunteerProfileId };
            _attendanceRepository.AddAttendanceRecord(record);
        }
        if (!Enum.TryParse<AttendanceStatus>(request.NewStatus, true, out var newStatus)) return Result.Failure(new Error("Attendance.InvalidStatus", "Invalid attendance status."));
        record.Status = newStatus;
        record.CheckInAt = request.CheckInAt;
        record.CheckOutAt = request.CheckOutAt;
        record.OverrideReason = request.Reason;
        record.OverrideByUserId = organizerId;
        record.OverrideAt = DateTime.UtcNow;
        if (record.CheckInAt.HasValue && record.CheckOutAt.HasValue) record.ApprovedHours = Math.Round((record.CheckOutAt.Value - record.CheckInAt.Value).TotalHours, 2);
        _attendanceRepository.UpdateAttendanceRecord(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<List<AttendanceRecordResponse>>> GetMyAttendanceAsync(Guid volunteerProfileId, CancellationToken cancellationToken = default)
    {
        var records = await _attendanceRepository.GetRecordsByVolunteerAsync(volunteerProfileId, cancellationToken);
        return Result.Success(records.Select(Map).ToList());
    }

    public async Task<Result<List<AttendanceRecordResponse>>> GetEventAttendanceAsync(Guid organizerId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var ev = await _eventRepository.GetDetailsByIdAsync(eventId, cancellationToken);
        if (ev == null || ev.OrganizerId != organizerId) return Result.Failure<List<AttendanceRecordResponse>>(Error.NotFound);
        var records = await _attendanceRepository.GetRecordsByEventAsync(eventId, cancellationToken);
        return Result.Success(records.Select(Map).ToList());
    }

    private static AttendanceRecordResponse Map(AttendanceRecord record) => new()
    {
        Id = record.Id,
        EventId = record.EventId,
        VolunteerProfileId = record.VolunteerProfileId,
        EventTitle = record.Event?.Title ?? string.Empty,
        CheckInAt = record.CheckInAt,
        CheckOutAt = record.CheckOutAt,
        Status = record.Status.ToString(),
        ApprovedHours = record.ApprovedHours
    };
}
