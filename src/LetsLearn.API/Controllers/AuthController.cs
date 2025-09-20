using System;
using LetsLearn.Infrastructure.Services.Auth;
using LetsLearn.UseCases.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public IActionResult Register([FromBody] SignUpRequestDTO request)
        {
            _authService.Register(request, HttpContext);
            return Ok(new SuccessResponseDTO("Successfully registered"));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] AuthRequestDTO request)
        {
            var response = _authService.Login(request, HttpContext);
            return Ok(response); 
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh()
        {
            try
            {
                _refreshTokenService.RefreshToken(HttpContext);
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
