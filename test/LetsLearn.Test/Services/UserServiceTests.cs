using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.User;
using System.Linq.Expressions;
using System.Threading;

namespace LetsLearn.Test.Services
{
    public class UserServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User)null!);

            var svc = new UserService(uow.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateAsync_UsernameAndAvatarUpdated_Commits()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var user = new User { Id = Guid.NewGuid(), Username = "old", Avatar = "old" };
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new UserService(uow.Object);

            var dto = new UpdateUserDTO { Username = " newName ", Avatar = " newAvatar " };
            var resp = await svc.UpdateAsync(user.Id, dto);

            uow.Verify(x => x.CommitAsync(), Times.Once);
            Assert.Equal("newName", resp.Username);
            Assert.Equal("newAvatar", resp.Avatar);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedList()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.FindAsync(
                        It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                        It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<User>
                 {
                     new User { Id = Guid.NewGuid(), Email = "a@b.com", Username = "A" },
                     new User { Id = Guid.NewGuid(), Email = "c@d.com", Username = "B" }
                 });

            var svc = new UserService(uow.Object);
            var list = await svc.GetAllAsync(Guid.NewGuid());

            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetByIdAsync_UserFound_ReturnsDto()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var user = new User { Id = Guid.NewGuid(), Email = "e@x.com", Username = "name", Avatar = "a", Role = "r" };
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var svc = new UserService(uow.Object);
            var dto = await svc.GetByIdAsync(user.Id);

            Assert.Equal(user.Id, dto.Id);
            Assert.Equal(user.Email, dto.Email);
        }

        [Fact]
        public async Task UpdateAsync_OnlyUsername_CommitsAndUpdates()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var ent = new User { Id = Guid.NewGuid(), Username = "old", Avatar = "oldA" };
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.GetByIdAsync(ent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ent);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new UserService(uow.Object);
            var resp = await svc.UpdateAsync(ent.Id, new UpdateUserDTO { Username = " new ", Avatar = "  " });

            uow.Verify(x => x.CommitAsync(), Times.Once);
            Assert.Equal("new", resp.Username);
            Assert.Equal("oldA", resp.Avatar);
        }

        [Fact]
        public async Task UpdateAsync_OnlyAvatar_CommitsAndUpdates()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var ent = new User { Id = Guid.NewGuid(), Username = "old", Avatar = "oldA" };
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.GetByIdAsync(ent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ent);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new UserService(uow.Object);
            var resp = await svc.UpdateAsync(ent.Id, new UpdateUserDTO { Username = "  ", Avatar = " newA " });

            uow.Verify(x => x.CommitAsync(), Times.Once);
            Assert.Equal("old", resp.Username);
            Assert.Equal("newA", resp.Avatar);
        }

        [Fact]
        public async Task UpdateAsync_NoFieldsProvided_Commits()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var ent = new User { Id = Guid.NewGuid(), Username = "old", Avatar = "oldA" };
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.GetByIdAsync(ent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ent);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new UserService(uow.Object);
            var resp = await svc.UpdateAsync(ent.Id, new UpdateUserDTO { Username = null, Avatar = null });

            uow.Verify(x => x.CommitAsync(), Times.Once);
            Assert.Equal("old", resp.Username);
            Assert.Equal("oldA", resp.Avatar);
        }
    }
}
