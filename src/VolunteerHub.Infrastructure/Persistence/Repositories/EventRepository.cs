using Microsoft.EntityFrameworkCore;
using VolunteerHub.Application.Abstractions;
using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetDetailsByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.SkillRequirements)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.SkillRequirements)
            .Where(e => e.OrganizerId == organizerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Event>> SearchPublishedAsync(string? keyword, DateTime? dateFrom, DateTime? dateTo, string? location, CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Include(e => e.SkillRequirements)
            .Where(e => e.Status == EventStatus.Published)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.Title, $"%{normalizedKeyword}%") ||
                EF.Functions.Like(e.Description, $"%{normalizedKeyword}%"));
        }

        if (dateFrom.HasValue)
            query = query.Where(e => e.StartAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.StartAt <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(location))
        {
            var normalizedLocation = location.Trim();
            query = query.Where(e => EF.Functions.Like(e.Address, $"%{normalizedLocation}%"));
        }

        return await query
            .OrderBy(e => e.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetPublishedByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.SkillRequirements)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Status == EventStatus.Published, cancellationToken);
    }

    public void Add(Event ev)
    {
        _context.Events.Add(ev);
    }

    public void Update(Event ev)
    {
        _context.Events.Update(ev);
    }
}
