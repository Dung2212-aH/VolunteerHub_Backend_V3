using Microsoft.EntityFrameworkCore;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _context;
    public AttendanceRepository(AppDbContext context) { _context = context; }
    public void AddAttendanceRecord(AttendanceRecord record) => _context.AttendanceRecords.Add(record);
    public void UpdateAttendanceRecord(AttendanceRecord record) => _context.AttendanceRecords.Update(record);
    public async Task<AttendanceRecord?> GetRecordAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default)
        => await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.EventId == eventId && a.VolunteerProfileId == profileId, cancellationToken);
    public async Task<List<AttendanceRecord>> GetRecordsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await _context.AttendanceRecords.Include(a => a.VolunteerProfile).Where(a => a.EventId == eventId).ToListAsync(cancellationToken);
    public async Task<List<AttendanceRecord>> GetRecordsByVolunteerAsync(Guid profileId, CancellationToken cancellationToken = default)
        => await _context.AttendanceRecords.Include(a => a.Event).Where(a => a.VolunteerProfileId == profileId).ToListAsync(cancellationToken);
    public async Task<bool> HasApprovedAttendanceAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default)
        => await _context.AttendanceRecords.AnyAsync(a => a.EventId == eventId && a.VolunteerProfileId == profileId && a.Status == AttendanceStatus.Approved, cancellationToken);
    public async Task<double> GetApprovedHoursForEventAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default)
        => await _context.AttendanceRecords.Where(a => a.EventId == eventId && a.VolunteerProfileId == profileId && a.Status == AttendanceStatus.Approved).SumAsync(a => a.ApprovedHours ?? 0.0, cancellationToken);
    public async Task<double> GetTotalApprovedHoursAsync(Guid profileId, CancellationToken cancellationToken = default)
        => await _context.AttendanceRecords.Where(a => a.VolunteerProfileId == profileId && a.Status == AttendanceStatus.Approved).SumAsync(a => a.ApprovedHours ?? 0.0, cancellationToken);
}
