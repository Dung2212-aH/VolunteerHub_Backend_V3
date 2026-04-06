using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Abstractions;

public interface IAttendanceRepository
{
    void AddAttendanceRecord(AttendanceRecord record);
    void UpdateAttendanceRecord(AttendanceRecord record);
    Task<AttendanceRecord?> GetRecordAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default);
    Task<List<AttendanceRecord>> GetRecordsByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<List<AttendanceRecord>> GetRecordsByVolunteerAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<bool> HasApprovedAttendanceAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default);
    Task<double> GetTotalApprovedHoursAsync(Guid profileId, CancellationToken cancellationToken = default);
}
