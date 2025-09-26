using System;
using LetsLearn.UseCases.Services.Auth;
using LetsLearn.UseCases.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LetsLearn.UseCases.DTOs.AuthDTO;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly RefreshTokenService _refreshTokenService;

        public AuthController(AuthService authService, RefreshTokenService refreshTokenService)
        {
            _authService = authService;
            _refreshTokenService = refreshTokenService;
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] SignUpRequest request)
        {
            await _authService.RegisterAsync(request, HttpContext);
            return Ok(new { message = "Successfully registered" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var response = await _authService.LoginAsync(request, HttpContext);
            return Ok(response); 
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                await _refreshTokenService.RefreshTokenAsync(HttpContext);
                return Ok(new { message = "Access token refreshed" });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            _authService.Logout(HttpContext);
            return NoContent();
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok("You are an Admin!");
        }

        [HttpGet("student-only")]
        [Authorize(Roles = "Student")]
        public IActionResult StudentOnly()
        {
            return Ok("You are a Student!");
        }
    }
}
