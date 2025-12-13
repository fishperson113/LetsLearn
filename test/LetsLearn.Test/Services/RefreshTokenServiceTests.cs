using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
using LetsLearn.Infrastructure.Repository;
using LetsLearn.UseCases.Services.Auth;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class RefreshTokenServiceTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly RefreshTokenRepository _refreshTokenRepo;
        private readonly RefreshTokenService _service;

        public RefreshTokenServiceTests()
        {
            var options = new DbContextOptionsBuilder<LetsLearnContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new LetsLearnContext(options);

            _refreshTokenRepo = new RefreshTokenRepository(context);

            _tokenServiceMock = new Mock<ITokenService>();
            _uowMock = new Mock<IUnitOfWork>();

            _uowMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepo);
            _uowMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _service = new RefreshTokenService(
                _tokenServiceMock.Object,
                _uowMock.Object
            );
        }

        [Fact]
        public async Task RefreshTokenAsync_MissingToken_Throws()
        {
            var ctx = new DefaultHttpContext();

            _tokenServiceMock
                .Setup(t => t.GetToken(ctx, false))
                .Returns((string?)null);

            await Assert.ThrowsAsync<Exception>(() =>
                _service.RefreshTokenAsync(ctx));
        }

        [Fact]
        public async Task RefreshTokenAsync_TokenNotStored_Throws()
        {
            var userId = Guid.NewGuid();
            var ctx = new DefaultHttpContext();

            _tokenServiceMock
                .Setup(t => t.GetToken(ctx, false))
                .Returns("refresh");

            _tokenServiceMock
                .Setup(t => t.ValidateToken("refresh", false))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                new Claim("userID", userId.ToString()),
                new Claim(ClaimTypes.Role, "STUDENT")
                })));

            await Assert.ThrowsAsync<SecurityTokenException>(() =>
                _service.RefreshTokenAsync(ctx));
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_Throws()
        {
            var userId = Guid.NewGuid();
            var ctx = new DefaultHttpContext();

            await _refreshTokenRepo.AddOrUpdateAsync(new RefreshToken
            {
                UserId = userId,
                Token = "refresh",
                ExpiryDate = DateTime.UtcNow.AddMinutes(-1)
            });

            _tokenServiceMock.Setup(t => t.GetToken(ctx, false)).Returns("refresh");
            _tokenServiceMock.Setup(t => t.ValidateToken("refresh", false))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                new Claim("userID", userId.ToString()),
                new Claim(ClaimTypes.Role, "STUDENT")
                })));

            await Assert.ThrowsAsync<SecurityTokenException>(() =>
                _service.RefreshTokenAsync(ctx));
        }
    }
}
