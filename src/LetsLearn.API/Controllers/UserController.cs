using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.DTOs.AuthDTO;
using LetsLearn.UseCases.Services.Auth;
using LetsLearn.UseCases.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IUserService _userService;

        public UserController(AuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
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

        [HttpGet("me")]
        public async Task<IActionResult> GetSelfInformation()
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            var user = await _userService.GetByIdAsync(userId);
            return Ok(user);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateUserInformation([FromBody] UpdateUserDTO dto)
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            var updated = await _userService.UpdateAsync(userId, dto);
            return Ok(updated);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            var users = await _userService.GetAllAsync(userId);
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserInformation(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(user);
        }
    }
}
