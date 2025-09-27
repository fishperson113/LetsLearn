using LetsLearn.UseCases.DTOs.AuthDTO;
using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.Infrastructure.Repository;
using LetsLearn.Core.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace LetsLearn.UseCases.Services.Auth
{
    public class AuthService
    {
        private readonly TokenService _tokenService;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(TokenService tokenService, RefreshTokenService refreshTokenService, IUnitOfWork unitOfWork)
        {
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(SignUpRequest request, HttpContext context)
        {
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                throw new Exception("Email has been registered!");

            var user = new Core.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = request.Username,
                PasswordHash = HashPassword(request.Password),
                Role = request.Role
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CommitAsync();

            var accessToken = _tokenService.CreateAccessToken(user.Id, user.Role);
            var refreshToken = await _refreshTokenService.CreateAndStoreRefreshTokenAsync(user.Id, user.Role);  

            _tokenService.SetTokenCookies(context, accessToken, refreshToken);
        }

        public async Task<JwtTokenResponse> LoginAsync(AuthRequest request, HttpContext context)
        {
            var user = await ((UserRepository)_unitOfWork.Users).GetByEmailAsync(request.Email); 
            if (user == null)
                throw new Exception("Email not found!");

            if (!VerifyPassword(request.Password, user.PasswordHash))
                throw new Exception("Incorrect email or password!");

            var accessToken = _tokenService.CreateAccessToken(user.Id, user.Role);
            var refreshToken = await _refreshTokenService.CreateAndStoreRefreshTokenAsync(user.Id, user.Role);

            _tokenService.SetTokenCookies(context, accessToken, refreshToken);

            return new JwtTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task UpdatePasswordAsync(UpdatePassword request, Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found!");

            if (!VerifyPassword(request.OldPassword, user.PasswordHash))
                throw new Exception("Old password is not correct!");

            user.PasswordHash = HashPassword(request.NewPassword);
            await _unitOfWork.CommitAsync();  
        }

        public void Logout(HttpContext context)
        {
            _tokenService.RemoveAllTokens(context);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
