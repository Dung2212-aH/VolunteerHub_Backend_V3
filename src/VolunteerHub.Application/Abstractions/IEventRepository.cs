using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetDetailsByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<List<Event>> SearchPublishedAsync(string? keyword, DateTime? dateFrom, DateTime? dateTo, string? location, CancellationToken cancellationToken = default);
    Task<Event?> GetPublishedByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    void Add(Event ev);
    void Update(Event ev);
}
