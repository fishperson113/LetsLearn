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
using Microsoft.EntityFrameworkCore;

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

        // Test Case Estimation:
        // Decision points (D):
        // - Pure repo operations without branching: +0
        // D = 0 => Minimum Test Cases = D + 1 = 1
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

        // Test Case Estimation:
        // Decision points (D):
        // - if header missing or invalid (with ||): if +1, logical operator || +1
        // - catch (inner) around ValidateToken: +1
        // - if stored token invalid/expired (with two ||): if +1, two logical operators || +2
        // D = 6 => Minimum Test Cases = D + 1 = 7
        public async Task<JwtTokenResponse> RefreshTokenAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                throw new SecurityTokenException("Refresh token is missing or invalid!");
            }

            var refreshToken = authHeader.Substring("Bearer ".Length).Trim();

            // Xác thực token (dùng TokenService nếu cần, nhưng AddJwtBearer sẽ xử lý)
            ClaimsPrincipal userPrincipal;
            try
            {
                userPrincipal = _tokenService.ValidateToken(refreshToken, isAccessToken: false);
            }
            catch (Microsoft.IdentityModel.Tokens.SecurityTokenException ex)
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenException("Invalid refresh token format!", ex);
            }
            catch (ArgumentException ex)
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenException("Invalid refresh token format!", ex);
            }
            var idClaim = userPrincipal.Claims.FirstOrDefault(c => c.Type == "userID");
            var roleClaim = userPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (idClaim == null || roleClaim == null)
            {
                throw new SecurityTokenException("Required claims are missing in refresh token.");
            }
            var userId = Guid.Parse(idClaim.Value);
            var role = roleClaim.Value;

            var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
            if (storedToken == null || storedToken.Token != refreshToken || storedToken.ExpiryDate < DateTime.UtcNow)
            {
                throw new SecurityTokenException("Invalid or expired refresh token!");
            }

            await _unitOfWork.RefreshTokens.DeleteAsync(storedToken);

            var newAccessToken = _tokenService.CreateAccessToken(userId, role);
            var newRefreshToken = await CreateAndStoreRefreshTokenAsync(userId, role);

            try
            {
                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist refreshed tokens.", ex);
            }

            return new JwtTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
    }
}
