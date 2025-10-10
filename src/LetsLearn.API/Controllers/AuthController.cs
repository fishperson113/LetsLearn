using System;
using LetsLearn.UseCases.ServiceInterfaces;
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
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Register([FromBody] SignUpRequest request)
        {
            var result = await _authService.RegisterAsync(request, HttpContext);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var response = await _authService.LoginAsync(request, HttpContext);
            return Ok(response); 
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
                var response = await _authService.RefreshAsync(HttpContext);
                return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            await _authService.LogoutAsync(HttpContext,userId);
            return Ok("Logged out successfully");
        }

        [HttpPatch("me/password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePassword request)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
                await _authService.UpdatePasswordAsync(request, userId);
                return Ok(new { message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
