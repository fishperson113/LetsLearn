using LetsLearn.UseCases.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Test.Services
{
    public class TokenServiceTests
    {
        private readonly TokenService _service;

        public TokenServiceTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Secret"] = "THIS_IS_A_TEST_SECRET_KEY_32_BYTES_LONG",
                    ["Jwt:AccessTokenExpireSeconds"] = "3600",
                    ["Jwt:RefreshTokenExpireSeconds"] = "7200",
                    ["Jwt:AccessTokenCookie"] = "ACCESS",
                    ["Jwt:RefreshTokenCookie"] = "REFRESH",
                    ["Jwt:AuthPrefix"] = "Bearer_"
                })
                .Build();

            _service = new TokenService(config);
        }

        // ================= CREATE TOKEN =================

        [Fact]
        public void CreateAccessToken_Valid_ReturnsJwt()
        {
            var token = _service.CreateAccessToken(Guid.NewGuid(), "STUDENT");

            Assert.NotNull(token);
            Assert.Contains(".", token); // JWT format
        }

        [Fact]
        public void CreateRefreshToken_Valid_ReturnsJwt()
        {
            var token = _service.CreateRefreshToken(Guid.NewGuid(), "INSTRUCTOR");

            Assert.NotNull(token);
            Assert.Contains(".", token);
        }

        // ================= VALIDATE TOKEN =================

        [Fact]
        public void ValidateToken_ValidAccessToken_ReturnsClaimsPrincipal()
        {
            var userId = Guid.NewGuid();
            var token = _service.CreateAccessToken(userId, "STUDENT");

            var principal = _service.ValidateToken(token, true);

            Assert.NotNull(principal);
            Assert.Equal(userId.ToString(), principal.FindFirst("userID")?.Value);
            Assert.Equal("STUDENT", principal.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public void ValidateToken_InvalidToken_ThrowsSecurityTokenException()
        {
            Assert.Throws<SecurityTokenException>(() =>
                _service.ValidateToken("invalid-token", true));
        }

        // ================= EXPIRE =================

        [Fact]
        public void GetRefreshTokenExpireSeconds_ReturnsConfiguredValue()
        {
            var seconds = _service.GetRefreshTokenExpireSeconds();

            Assert.Equal(7200, seconds);
        }

        // ================= COOKIES =================

        [Fact]
        public void SetTokenCookies_SetsAccessAndRefreshCookies()
        {
            var context = new DefaultHttpContext();

            _service.SetTokenCookies(context, "access123", "refresh456");

            var setCookieHeaders = context.Response.Headers["Set-Cookie"].ToString();

            Assert.Contains("ACCESS=Bearer_access123", setCookieHeaders);
            Assert.Contains("REFRESH=Bearer_refresh456", setCookieHeaders);
        }

        [Fact]
        public void GetToken_FromCookies_ReturnsTokenWithoutPrefix()
        {
            var context = new DefaultHttpContext();
            context.Request.Cookies = new RequestCookieCollection(
                new Dictionary<string, string>
                {
                    { "ACCESS", "Bearer_token123" }
                });

            var token = _service.GetToken(context, isAccessToken: true);

            Assert.Equal("token123", token);
        }

        [Fact]
        public void GetToken_NoCookie_Throws()
        {
            var context = new DefaultHttpContext();

            Assert.Throws<Exception>(() =>
                _service.GetToken(context, true));
        }

        [Fact]
        public void GetToken_InvalidPrefix_Throws()
        {
            var context = new DefaultHttpContext();
            context.Request.Cookies = new RequestCookieCollection(
                new Dictionary<string, string>
                {
                    { "ACCESS", "INVALID_token" }
                });

            Assert.Throws<Exception>(() =>
                _service.GetToken(context, true));
        }

        [Fact]
        public void RemoveAllTokens_ExpiresCookies()
        {
            var context = new DefaultHttpContext();

            _service.RemoveAllTokens(context);

            var cookies = context.Response.Headers["Set-Cookie"].ToString();

            Assert.Contains("ACCESS=", cookies);
            Assert.Contains("REFRESH=", cookies);
            Assert.Contains("expires=", cookies.ToLower());
        }

        // ================= CONFIG =================

        [Fact]
        public void Constructor_MissingIssuer_Throws()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "THIS_IS_A_TEST_SECRET_KEY_32_BYTES_LONG"
                })
                .Build();

            Assert.Throws<ArgumentException>(() => new TokenService(config));
        }

        [Fact]
        public void Constructor_MissingSecret_Throws()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "issuer"
                })
                .Build();

            Assert.Throws<ArgumentException>(() => new TokenService(config));
        }
    }
}
