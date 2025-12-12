using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.Services;
using LetsLearn.UseCases.DTOs;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<IUserRepository> _users;
        private readonly Mock<INotificationRepository> _notifications;
        private readonly NotificationService _svc;

        public NotificationServiceTests()
        {
            _uow = new Mock<IUnitOfWork>();
            _users = new Mock<IUserRepository>();
            _notifications = new Mock<INotificationRepository>();

            _uow.Setup(x => x.Users).Returns(_users.Object);
            _uow.Setup(x => x.Notifications).Returns(_notifications.Object);

            _svc = new NotificationService(_uow.Object);
        }

        // ---------------- GET ----------------
        [Fact]
        public async Task GetNotificationsAsync_UserNotFound_Throws()
        {
            _users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.GetNotificationsAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetNotificationsAsync_Success_ReturnsDtos()
        {
            var userId = Guid.NewGuid();
            _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new User { Id = userId });

            _notifications.Setup(x =>
                x.GetByUserIdOrderByCreatedAtDescAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Notification>
                {
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = "GENERIC",
                        CreatedAt = DateTime.UtcNow,
                        Data = JsonSerializer.Serialize(new { title = "Hi", message = "Hello" })
                    }
                });

            var result = await _svc.GetNotificationsAsync(userId);

            Assert.Single(result);
            Assert.Equal("Hi", result[0].Title);
        }

        // ---------------- MARK AS READ ----------------
        [Fact]
        public async Task MarkAsReadAsync_NotFound_Throws()
        {
            _notifications.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((Notification?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.MarkAsReadAsync(Guid.NewGuid(), true));
        }

        [Fact]
        public async Task MarkAsReadAsync_ReadTrue_SetsReadAt()
        {
            var n = new Notification { Id = Guid.NewGuid() };
            _notifications.Setup(x => x.GetByIdAsync(n.Id, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(n);

            _uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var dto = await _svc.MarkAsReadAsync(n.Id, true);

            Assert.True(dto.IsRead);
            Assert.NotNull(n.ReadAt);
        }

        // ---------------- DELETE ----------------
        [Fact]
        public async Task DeleteNotificationAsync_NotFound_Throws()
        {
            _notifications.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((Notification?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.DeleteNotificationAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteNotificationAsync_Success_Deletes()
        {
            var n = new Notification { Id = Guid.NewGuid() };
            _notifications.Setup(x => x.GetByIdAsync(n.Id, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(n);

            _uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            await _svc.DeleteNotificationAsync(n.Id);

            _notifications.Verify(x => x.DeleteAsync(n), Times.Once);
        }

        // ---------------- CREATE ----------------
        [Fact]
        public async Task CreateNotificationAsync_UserNotFound_Throws()
        {
            _users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.CreateNotificationAsync(Guid.NewGuid(), "t", "m"));
        }

        [Fact]
        public async Task CreateNotificationAsync_Success_Creates()
        {
            var user = new User { Id = Guid.NewGuid() };
            _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user);

            _uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var dto = await _svc.CreateNotificationAsync(user.Id, "Title", "Msg");

            Assert.Equal("Title", dto.Title);
            Assert.False(dto.IsRead);
        }
    }
}
