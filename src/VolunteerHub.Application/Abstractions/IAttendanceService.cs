using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Requests;
using VolunteerHub.Contracts.Responses;

namespace VolunteerHub.Application.Abstractions;

public interface IAttendanceService
{
    Task<Result> CheckInAsync(Guid volunteerProfileId, CheckInRequest request, CancellationToken cancellationToken = default);
    Task<Result> CheckOutAsync(Guid volunteerProfileId, CheckOutRequest request, CancellationToken cancellationToken = default);
    Task<Result> ManualOverrideAsync(Guid organizerId, Guid eventId, ManualOverrideRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<AttendanceRecordResponse>>> GetMyAttendanceAsync(Guid volunteerProfileId, CancellationToken cancellationToken = default);
    Task<Result<List<AttendanceRecordResponse>>> GetEventAttendanceAsync(Guid organizerId, Guid eventId, CancellationToken cancellationToken = default);
}
