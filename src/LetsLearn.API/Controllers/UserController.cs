using System;
using LetsLearn.UseCases.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LetsLearn.UseCases.DTOs.AuthDTO;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly AuthService _authService;

        public UserController(AuthService authService)
        {
            _authService = authService;
        }

        [Authorize]
        [HttpPatch("me/password")]
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
