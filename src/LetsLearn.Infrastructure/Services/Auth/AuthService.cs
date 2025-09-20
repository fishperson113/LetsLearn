using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using LetsLearn.Core.Entities;
using LetsLearn.Core.Shared;
using LetsLearn.UseCases.DTOs;
using Microsoft.AspNetCore.Http;

namespace LetsLearn.Infrastructure.Services.Auth
{
    public class AuthService
    {
        private readonly TokenService _tokenService;
        private readonly RefreshTokenService _refreshTokenService;

        // In-memory user store
        private static readonly ConcurrentDictionary<string, (Guid UserId, string Email, string Username, string PasswordHash, string Role)> _users
            = new();

        public AuthService(TokenService tokenService, RefreshTokenService refreshTokenService)
        {
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
        }

        public void Register(SignUpRequestDTO request, HttpContext context)
        {
            if (_users.ContainsKey(request.Email))
                throw new Exception("Email has been registered!");

            var userId = Guid.NewGuid();
            var passwordHash = HashPassword(request.Password);

            _users[request.Email] = (userId, request.Email, request.Username, passwordHash, request.Role);

            // create tokens + set cookies
            var accessToken = _tokenService.CreateAccessToken(userId, request.Role);
            var refreshToken = _refreshTokenService.CreateAndStoreRefreshToken(userId, request.Role);

            _tokenService.SetTokenCookies(context, accessToken, refreshToken);
        }

        public JwtTokenResponse Login(AuthRequestDTO request, HttpContext context)
        {
            if (!_users.TryGetValue(request.Email, out var user))
                throw new Exception("Email not found!");

            if (!VerifyPassword(request.Password, user.PasswordHash))
                throw new Exception("Incorrect email or password!");

            var accessToken = _tokenService.CreateAccessToken(user.UserId, user.Role);
            var refreshToken = _refreshTokenService.CreateAndStoreRefreshToken(user.UserId, user.Role);

            _tokenService.SetTokenCookies(context, accessToken, refreshToken);

            return new JwtTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public void UpdatePassword(UpdatePasswordDTO request, Guid userId)
        {
            var userEntry = _users.Values.FirstOrDefault(u => u.UserId == userId);
            if (userEntry.UserId == Guid.Empty)
                throw new Exception("User not found!");

            if (!VerifyPassword(request.OldPassword, userEntry.PasswordHash))
                throw new Exception("Old password is not correct!");

            var newHash = HashPassword(request.NewPassword);
            _users[userEntry.Email] = (userEntry.UserId, userEntry.Email, userEntry.Username, newHash, userEntry.Role);
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
