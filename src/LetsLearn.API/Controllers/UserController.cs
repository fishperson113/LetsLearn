﻿using LetsLearn.Core.Shared;
using LetsLearn.UseCases.DTOs;
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
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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
            var userIdClaim = User.FindFirst("uid")
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)
                                  ?? User.FindFirst("sub");

            if (userIdClaim is null)
                return Unauthorized(new { error = "Missing user id claim (token not present or invalid)." });

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
                return BadRequest(new { error = "Invalid user id format in claims." });
            var updated = await _userService.UpdateAsync(userId, dto);
            return Ok(updated);
        }

        [HttpGet("all")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
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
