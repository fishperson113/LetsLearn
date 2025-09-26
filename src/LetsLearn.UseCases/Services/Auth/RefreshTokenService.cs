using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
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
using System.Security.Claims;
using LetsLearn.Infrastructure.Repository;

namespace LetsLearn.UseCases.Services.Auth
{
    public class RefreshTokenService
    {
        private readonly TokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenService(TokenService tokenService, IUnitOfWork unitOfWork)
        {
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> CreateAndStoreRefreshTokenAsync(Guid userId, string role)
        {
            var refreshToken = _tokenService.CreateRefreshToken(userId, role);
            var expirySeconds = int.Parse(_tokenService.RefreshTokenExpireSeconds.ToString()); 
            var tokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddSeconds(expirySeconds)
            };

            await((RefreshTokenRepository)_unitOfWork.RefreshTokens).AddOrUpdateAsync(tokenEntity);
            await _unitOfWork.CommitAsync();

            return refreshToken;
        }

        public async Task RefreshTokenAsync(HttpContext context)
        {
            var refreshToken = _tokenService.GetToken(context, isAccessToken: false);
            if (string.IsNullOrEmpty(refreshToken))
                throw new Exception("Refresh token is missing!");

            var userPrincipal = _tokenService.ValidateToken(refreshToken, isAccessToken: false);

            var userId = Guid.Parse(userPrincipal.Claims.First(c => c.Type == "userID").Value);
            var role = userPrincipal.Claims.First(c => c.Type == ClaimTypes.Role).Value;

            var storedToken = await ((RefreshTokenRepository)_unitOfWork.RefreshTokens).GetByUserIdAsync(userId);
            if (storedToken == null || storedToken.Token != refreshToken || storedToken.ExpiryDate < DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token!");

            var newAccessToken = _tokenService.CreateAccessToken(userId, role);
            var newRefreshToken = await CreateAndStoreRefreshTokenAsync(userId, role);

            _tokenService.SetTokenCookies(context, newAccessToken, newRefreshToken);
        }
    }
}
