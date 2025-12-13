using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services.Auth;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Test.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IUserRepository> _userRepoMock = new();

        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

            _service = new AuthService(
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                _uowMock.Object
            );
        }

        // ================= REGISTER =================

        [Fact]
        public async Task RegisterAsync_EmailExists_Throws()
        {
            _userRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterAsync(new SignUpRequest
                {
                    Email = "test@mail.com"
                }, new DefaultHttpContext()));
        }

        [Fact]
        public async Task RegisterAsync_Valid_CreatesUserAndSetsCookies()
        {
            _userRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _userRepoMock
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _uowMock
                .Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            _tokenServiceMock
                .Setup(t => t.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns("access-token");

            _refreshTokenServiceMock
                .Setup(r => r.CreatRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync("refresh-token");

            var ctx = new DefaultHttpContext();

            await _service.RegisterAsync(new SignUpRequest
            {
                Email = "test@mail.com",
                Username = "test",
                Password = "123",
                Role = "STUDENT"
            }, ctx);

            _tokenServiceMock.Verify(t =>
                t.SetTokenCookies(ctx, "access-token", "refresh-token"),
                Times.Once);
        }

        // ================= REFRESH =================

        [Fact]
        public async Task RefreshAsync_CallsRefreshService()
        {
            var ctx = new DefaultHttpContext();

            await _service.RefreshAsync(ctx);

            _refreshTokenServiceMock.Verify(r =>
                r.RefreshTokenAsync(ctx), Times.Once);
        }

        // ================= UPDATE PASSWORD =================

        [Fact]
        public async Task UpdatePasswordAsync_UserNotFound_Throws()
        {
            _userRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdatePasswordAsync(new UpdatePassword(), Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdatePasswordAsync_WrongOldPassword_Throws()
        {
            var user = new User
            {
                PasswordHash = "HASH"
            };

            _userRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdatePasswordAsync(new UpdatePassword
                {
                    OldPassword = "wrong"
                }, Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdatePasswordAsync_Valid_Commits()
        {
            var oldPwd = "old";
            var oldHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create()
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(oldPwd)));

            var user = new User { PasswordHash = oldHash };

            _userRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _uowMock
                .Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            await _service.UpdatePasswordAsync(new UpdatePassword
            {
                OldPassword = oldPwd,
                NewPassword = "new"
            }, Guid.NewGuid());

            _uowMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ================= LOGOUT =================

        [Fact]
        public void Logout_RemovesCookies()
        {
            var ctx = new DefaultHttpContext();

            _service.Logout(ctx);

            _tokenServiceMock.Verify(t =>
                t.RemoveAllTokens(ctx), Times.Once);
        }
    }
}
