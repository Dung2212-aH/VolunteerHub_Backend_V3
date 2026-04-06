using VolunteerHub.Application.Abstractions;
using VolunteerHub.Application.Common;
using VolunteerHub.Contracts.Notification;
using VolunteerHub.Domain.Entities;
using VolunteerHub.Domain.Enums;

namespace VolunteerHub.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository repository, IEmailSender emailSender, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
    }

    public Task SendWelcomeNotificationAsync(Guid userId, string email, string firstName, CancellationToken cancellationToken = default)
        => CreateSimpleAsync(userId, NotificationType.Welcome, "Welcome", $"Welcome to Volunteer Hub, {firstName}!", null, null, cancellationToken);
    public async Task SendEmailConfirmationNotificationAsync(Guid userId, string email, string confirmationLink, CancellationToken cancellationToken = default)
        => await _emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your email: {confirmationLink}", cancellationToken);
    public async Task SendPasswordResetNotificationAsync(Guid userId, string email, string resetLink, CancellationToken cancellationToken = default)
        => await _emailSender.SendEmailAsync(email, "Reset your password", $"Reset your password: {resetLink}", cancellationToken);
    public Task NotifyApplicationSubmittedAsync(Guid userId, string eventTitle, Guid applicationId, CancellationToken cancellationToken = default)
        => CreateSimpleAsync(userId, NotificationType.ApplicationSubmitted, "Application submitted", $"Your application for {eventTitle} was submitted.", "EventApplication", applicationId, cancellationToken);
    public Task NotifyApplicationApprovedAsync(Guid userId, string eventTitle, Guid applicationId, CancellationToken cancellationToken = default)
        => CreateSimpleAsync(userId, NotificationType.ApplicationApproved, "Application approved", $"Your application for {eventTitle} was approved.", "EventApplication", applicationId, cancellationToken);
    public Task NotifyApplicationRejectedAsync(Guid userId, string eventTitle, Guid applicationId, string? reason, CancellationToken cancellationToken = default)
        => CreateSimpleAsync(userId, NotificationType.ApplicationRejected, "Application rejected", $"Your application for {eventTitle} was rejected. {reason}".Trim(), "EventApplication", applicationId, cancellationToken);
    public Task NotifyCertificateIssuedAsync(Guid userId, string eventTitle, Guid certificateId, CancellationToken cancellationToken = default)
        => CreateSimpleAsync(userId, NotificationType.CertificateIssued, "Certificate issued", $"Your certificate for {eventTitle} is now available.", "Certificate", certificateId, cancellationToken);
    public Task NotifyBadgeAwardedAsync(Guid userId, string badgeName, Guid badgeId, CancellationToken cancellationToken = default)
        => CreateSimpleAsync(userId, NotificationType.BadgeAwarded, "Badge awarded", $"You earned the badge {badgeName}.", "Badge", badgeId, cancellationToken);

    public async Task<Result<List<NotificationListItemResponse>>> GetMyNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Result.Success((await _repository.GetByUserIdAsync(userId, cancellationToken)).Select(n => new NotificationListItemResponse { Id = n.Id, Type = n.Type.ToString(), Title = n.Title, Status = n.Status.ToString(), IsRead = n.Status == NotificationStatus.Read, CreatedAt = n.CreatedAt }).ToList());
    public async Task<Result<int>> GetMyUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => Result.Success(await _repository.GetUnreadCountAsync(userId, cancellationToken));
    public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null || notification.UserId != userId) return Result.Failure(Error.NotFound);
        notification.Status = NotificationStatus.Read;
        notification.ReadAt = DateTime.UtcNow;
        _repository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task CreateSimpleAsync(Guid userId, NotificationType type, string title, string message, string? relatedEntityType, Guid? relatedEntityId, CancellationToken cancellationToken)
    {
        _repository.Add(new Notification { UserId = userId, Type = type, Channel = NotificationChannel.InApp, Title = title, Message = message, Status = NotificationStatus.Sent, SentAt = DateTime.UtcNow, RelatedEntityType = relatedEntityType, RelatedEntityId = relatedEntityId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
