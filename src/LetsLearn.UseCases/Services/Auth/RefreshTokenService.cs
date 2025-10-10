using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
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
using LetsLearn.UseCases.ServiceInterfaces;

namespace LetsLearn.UseCases.Services.Auth
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenService(ITokenService tokenService, IUnitOfWork unitOfWork)
        {
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> CreateAndStoreRefreshTokenAsync(Guid userId, string role)
        {
            var refreshToken = _tokenService.CreateRefreshToken(userId, role);
            var expirySeconds = int.Parse(_tokenService.GetRefreshTokenExpireSeconds().ToString());
            var tokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddSeconds(expirySeconds)
            };

            await ((RefreshTokenRepository)_unitOfWork.RefreshTokens).AddOrUpdateAsync(tokenEntity);
            await _unitOfWork.CommitAsync();

            return refreshToken;
        }

        public async Task<JwtTokenResponse> RefreshTokenAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                throw new Exception("Refresh token is missing or invalid!");
            }

            var refreshToken = authHeader.Substring("Bearer ".Length).Trim();

            // Xác thực token (dùng TokenService nếu cần, nhưng AddJwtBearer sẽ xử lý)
            ClaimsPrincipal userPrincipal;
            try
            {
                userPrincipal = _tokenService.ValidateToken(refreshToken, isAccessToken: false);
            }
            catch
            {
                throw new Exception("Invalid refresh token format!");
            }
            var userId = Guid.Parse(userPrincipal.Claims.First(c => c.Type == "userID").Value);
            var role = userPrincipal.Claims.First(c => c.Type == ClaimTypes.Role).Value;

            var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
            if (storedToken == null || storedToken.Token != refreshToken || storedToken.ExpiryDate < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired refresh token!");
            }

            await _unitOfWork.RefreshTokens.DeleteAsync(storedToken);

            var newAccessToken = _tokenService.CreateAccessToken(userId, role);
            var newRefreshToken = await CreateAndStoreRefreshTokenAsync(userId, role);

            await _unitOfWork.CommitAsync();

            return new JwtTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
    }
}
