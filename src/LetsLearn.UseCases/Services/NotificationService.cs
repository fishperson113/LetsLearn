using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LetsLearn.UseCases.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;

        public NotificationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId)
                       ?? throw new KeyNotFoundException("User not found");

            var list = await _uow.Notifications.GetByUserIdOrderByCreatedAtDescAsync(userId, ct);

            return list.Select(ToDto).ToList();
        }

        public async Task<NotificationDto> MarkAsReadAsync(Guid id, bool isRead, CancellationToken ct = default)
        {
            var notification = await _uow.Notifications.GetByIdAsync(id)
                              ?? throw new KeyNotFoundException("Notification not found");

            notification.ReadAt = isRead ? DateTime.UtcNow : null;

            await _uow.CommitAsync();

            return ToDto(notification);
        }

        public async Task DeleteNotificationAsync(Guid id, CancellationToken ct = default)
        {
            var notification = await _uow.Notifications.GetByIdAsync(id)
                              ?? throw new KeyNotFoundException("Notification not found");

            await _uow.Notifications.DeleteAsync(notification);
            await _uow.CommitAsync();
        }

        public async Task<NotificationDto> CreateNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(userId)
                       ?? throw new KeyNotFoundException("User not found");

            var payload = new { title, message };

            var entity = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "GENERIC",
                EntityId = Guid.Empty,
                ActorId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null,
                Data = JsonSerializer.Serialize(payload)
            };

            await _uow.Notifications.AddAsync(entity);
            await _uow.CommitAsync();

            return ToDto(entity);
        }

        public async Task NotifyUserAsync(Guid userId, string title, string message, CancellationToken ct = default)
        {
            await CreateNotificationAsync(userId, title, message, ct);
        }

        // ===== Mapping =====
        private static NotificationDto ToDto(Notification n)
        {
            string? title = null;
            string? message = null;

            if (!string.IsNullOrWhiteSpace(n.Data))
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<NotificationPayload>(n.Data);
                    title = obj?.Title;
                    message = obj?.Message;
                }
                catch
                {
                    // ignore parse error, fallback below
                }
            }

            // fallback 
            title ??= n.Type;
            message ??= n.Data;

            return new NotificationDto
            {
                Id = n.Id,
                Title = title,
                Message = message,
                Timestamp = n.CreatedAt,
                IsRead = n.ReadAt != null
            };
        }

        private class NotificationPayload
        {
            public string? Title { get; set; }
            public string? Message { get; set; }
        }
    }
}
