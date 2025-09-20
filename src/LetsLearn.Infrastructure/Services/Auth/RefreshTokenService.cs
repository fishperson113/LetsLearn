using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LetsLearn.Core.Entities;
using System.Security.Claims;

namespace LetsLearn.Infrastructure.Services.Auth
{
    public class RefreshTokenService
    {
        private readonly TokenService _tokenService;

        private static readonly ConcurrentDictionary<Guid, string> _refreshTokens = new();

        public RefreshTokenService(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public string CreateAndStoreRefreshToken(Guid userId, string role)
        {
            var refreshToken = _tokenService.CreateRefreshToken(userId, role);
            _refreshTokens[userId] = refreshToken;
            return refreshToken;
        }

        public void RefreshToken(HttpContext context)
        {
            var refreshToken = _tokenService.GetToken(context, isAccessToken: false);
            if (string.IsNullOrEmpty(refreshToken))
                throw new Exception("Refresh token is missing!");

            var userPrincipal = _tokenService.ValidateToken(refreshToken, isAccessToken: false);

            var userId = Guid.Parse(userPrincipal.Claims.First(c => c.Type == "userID").Value);
            var role = userPrincipal.Claims.First(c => c.Type == ClaimTypes.Role).Value;

            if (!_refreshTokens.TryGetValue(userId, out var storedRefresh) || storedRefresh != refreshToken)
                throw new Exception("Invalid refresh token!");

            var newAccessToken = _tokenService.CreateAccessToken(userId, role);
            var newRefreshToken = CreateAndStoreRefreshToken(userId, role);

            _tokenService.SetTokenCookies(context, newAccessToken, newRefreshToken);
        }
    }
}
