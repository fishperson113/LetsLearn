using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services.Auth;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Security.Cryptography;

namespace LetsLearn.Test.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task RegisterAsync_EmailExists_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            userRepo.Setup(x => x.FirstOrDefaultAsync(
                        It.IsAny<Expression<Func<User, bool>>>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User());

            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterAsync(new SignUpRequest { Email = "a@b.com", Password = "x", Username = "u", Role = "r" }, new DefaultHttpContext()));
        }

        [Fact]
        public async Task UpdatePasswordAsync_UserNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);

            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdatePasswordAsync(new UpdatePassword { OldPassword = "old", NewPassword = "new" }, Guid.NewGuid()));
        }

        [Fact]
        public async Task RegisterAsync_Valid_ReturnsTokens()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            userRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((User)null!);
            userRepo.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var tokenSvc = new Mock<ITokenService>();
            tokenSvc.Setup(x => x.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("access");
            var refreshSvc = new Mock<IRefreshTokenService>();
            refreshSvc.Setup(x => x.CreateAndStoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                      .ReturnsAsync("refresh");
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            var res = await svc.RegisterAsync(new SignUpRequest { Email = "a@b.com", Password = "x", Username = "u", Role = "r" }, new DefaultHttpContext());
            Assert.Equal("access", res.AccessToken);
            Assert.Equal("refresh", res.RefreshToken);
        }

        [Fact]
        public async Task UpdatePasswordAsync_OldInvalid_ThrowsUnauthorized()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            using var sha256 = SHA256.Create();
            var user = new User { Id = Guid.NewGuid(), PasswordHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes("old"))) };
            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.UpdatePasswordAsync(new UpdatePassword { OldPassword = "wrong", NewPassword = "new" }, user.Id));
        }
        [Fact]
        public async Task UpdatePasswordAsync_Valid_Commits()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            using var sha256 = SHA256.Create();
            var user = new User { Id = Guid.NewGuid(), PasswordHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes("old"))) };
            userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await svc.UpdatePasswordAsync(new UpdatePassword { OldPassword = "old", NewPassword = "new" }, user.Id);
            uow.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_UserIdEmpty_NoCommit()
        {
            var uow = new Mock<IUnitOfWork>();
            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await svc.LogoutAsync(new DefaultHttpContext(), Guid.Empty);
            uow.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_UserIdSet_NoStored_NoCommit()
        {
            var uow = new Mock<IUnitOfWork>();
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            uow.Setup(x => x.RefreshTokens).Returns(refreshRepo.Object);
            refreshRepo.Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken)null!);

            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await svc.LogoutAsync(new DefaultHttpContext(), Guid.NewGuid());
            uow.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_UserIdSet_WithStored_DeletesAndCommits()
        {
            var uow = new Mock<IUnitOfWork>();
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            uow.Setup(x => x.RefreshTokens).Returns(refreshRepo.Object);
            refreshRepo.Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RefreshToken { UserId = Guid.NewGuid(), Token = "t" });
            refreshRepo.Setup(x => x.DeleteAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var tokenSvc = new Mock<ITokenService>();
            var refreshSvc = new Mock<IRefreshTokenService>();
            var svc = new AuthService(tokenSvc.Object, refreshSvc.Object, uow.Object);

            await svc.LogoutAsync(new DefaultHttpContext(), Guid.NewGuid());
            uow.Verify(x => x.CommitAsync(), Times.Once);
        }
    }
}
