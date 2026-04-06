using VolunteerHub.Domain.Entities;

namespace VolunteerHub.Application.Abstractions;

public interface INotificationRepository
{
    void Add(Notification notification);
    void Update(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
