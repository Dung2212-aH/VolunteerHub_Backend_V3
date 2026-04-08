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

        var methodResult = ParseMethod(request.Method, request.Latitude, request.Longitude, ev);
        if (!methodResult.IsSuccess) return Result.Failure(methodResult.Error);

        var methodType = methodResult.Value;
        if (methodType == CheckInMethod.GPS)
        {
            var eventLatitude = ev.Latitude!.Value;
            var eventLongitude = ev.Longitude!.Value;
            var requestLatitude = request.Latitude!.Value;
            var requestLongitude = request.Longitude!.Value;
            var dist = LocationHelper.CalculateDistanceKm(eventLatitude, eventLongitude, requestLatitude, requestLongitude);
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

        var methodResult = ParseMethod(request.Method, request.Latitude, request.Longitude, ev);
        if (!methodResult.IsSuccess) return Result.Failure(methodResult.Error);

        var record = await _attendanceRepository.GetRecordAsync(request.EventId, volunteerProfileId, cancellationToken);
        if (record == null || !record.CheckInAt.HasValue) return Result.Failure(new Error("Attendance.NoCheckIn", "You must check in first."));
        if (record.Status != AttendanceStatus.CheckedIn) return Result.Failure(new Error("Attendance.InvalidStatus", "Attendance must be in CheckedIn status before check-out."));

        var methodType = methodResult.Value;
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

    public async Task<Result> ApproveAttendanceAsync(Guid organizerId, Guid eventId, Guid volunteerProfileId, CancellationToken cancellationToken = default)
    {
        var ev = await _eventRepository.GetDetailsByIdAsync(eventId, cancellationToken);
        if (ev == null || ev.OrganizerId != organizerId) return Result.Failure(Error.NotFound);

        var record = await _attendanceRepository.GetRecordAsync(eventId, volunteerProfileId, cancellationToken);
        if (record == null) return Result.Failure(Error.NotFound);

        if (record.Status == AttendanceStatus.Approved) return Result.Success();

        if (record.Status != AttendanceStatus.CheckedOut || !record.CheckInAt.HasValue || !record.CheckOutAt.HasValue)
            return Result.Failure(new Error("Attendance.InvalidStatus", "Only checked-out attendance records can be approved."));

        record.Status = AttendanceStatus.Approved;
        record.OverrideByUserId = organizerId;
        record.OverrideAt = DateTime.UtcNow;
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

    private static Result<CheckInMethod> ParseMethod(string method, double? latitude, double? longitude, Event ev)
    {
        if (!Enum.TryParse<CheckInMethod>(method, true, out var parsedMethod) || parsedMethod == CheckInMethod.None)
            return Result.Failure<CheckInMethod>(new Error("Attendance.InvalidMethod", "Attendance method must be QR, GPS, or Manual."));

        if (parsedMethod == CheckInMethod.GPS)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return Result.Failure<CheckInMethod>(new Error("Attendance.MissingCoordinates", "GPS attendance requires latitude and longitude."));

            if (!ev.Latitude.HasValue || !ev.Longitude.HasValue)
                return Result.Failure<CheckInMethod>(new Error("Attendance.EventLocationMissing", "This event does not have GPS coordinates configured."));
        }

        return Result.Success(parsedMethod);
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
