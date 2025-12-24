using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services.CourseClone;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CourseController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICourseCloneService _courseCloneService;

        public CourseController(ICourseService courseService, ILogger<CourseController> logger, IConfiguration configuration, ICourseCloneService courseCloneService)
        {
            _courseService = courseService;
            _logger = logger;
            _configuration = configuration;
            _courseCloneService = courseCloneService;
        }

        [HttpGet("{courseId}/meeting/{topicId}/token")]
        public async Task<ActionResult> GetMeetingToken(string courseId, string topicId, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);

                // Verify user has access to this course by checking if they have any work in it
                // This uses existing service method as a simple access check
                var userWorks = await _courseService.GetAllWorksOfCourseAndUserAsync(courseId, userId, null, null, null, ct);
                if (userWorks == null)
                {
                    return Forbid("Access denied to course");
                }

                // Generate LiveKit JWT token
                var liveKitApiKey = _configuration["LiveKit:ApiKey"] ?? "devkey";
                var liveKitApiSecret = _configuration["LiveKit:ApiSecret"] ?? "thisisaverylongsecretstring1234567890";
                var liveKitWsUrl = _configuration["LiveKit:WsUrl"] ?? "ws://45.128.222.24:7880";

                var token = CreateLiveKitToken(userId.ToString(), User.Identity?.Name ?? $"User-{userId}", topicId, liveKitApiKey, liveKitApiSecret);

                return Ok(new
                {
                    token = token,
                    roomName = topicId,
                    wsUrl = liveKitWsUrl
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = "Meeting not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating meeting token for course {CourseId}, topic {TopicId}", courseId, topicId);
                return BadRequest(new { error = "Failed to generate meeting token" });
            }
        }

        private string CreateLiveKitToken(string userId, string userName, string roomName, string apiKey, string apiSecret)
        {
            var now = DateTimeOffset.UtcNow;
            var nbf = now; // Not before - token is valid immediately
            var exp = now.AddHours(24); // Token expires in 24 hours

            // Create video permissions as proper object for JSON serialization
            var videoPermissions = new Dictionary<string, object>
            {
                {"room", roomName},
                {"roomJoin", true},
                {"canPublish", true},
                {"canSubscribe", true}
            };

            // Serialize video permissions to JSON
            var videoJson = JsonSerializer.Serialize(videoPermissions);

            // Create claims for LiveKit JWT
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Iss, apiKey),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Nbf, nbf.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, exp.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("name", userName),
                new Claim("video", videoJson, JsonClaimValueTypes.Json)
            };

            var key = Encoding.UTF8.GetBytes(apiSecret);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: apiKey,
                claims: claims,
                notBefore: nbf.DateTime,
                expires: exp.DateTime,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        /// <summary>
        /// GET /api/course?userId=...
        /// Retrieves a list of courses.
        /// - If <paramref name="userId"/> is provided, returns courses created by that user.  
        /// - If no userId is provided, returns all published (public) courses.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetCourseResponse>>> GetAllCoursesByUserId([FromQuery] Guid? userId, CancellationToken ct)
        {
            try
            {
                if (userId.HasValue && userId.Value != Guid.Empty)
                {
                    var coursesByUser = await _courseService.GetAllCoursesByUserIdAsync(userId.Value, ct);
                    return Ok(coursesByUser);
                }

                var publics = await _courseService.GetAllCoursesAsync(ct);
                return Ok(publics);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "GET courses not found. userId={UserId}", userId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/course/{id}
        /// Retrieves the details of a specific course by ID.
        /// </summary>
        [HttpGet("{id}", Name = "GetCourseById")]
        [AllowAnonymous]
        public async Task<ActionResult<GetCourseResponse>> GetById(String id, CancellationToken ct)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id, ct);
                return Ok(course);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/course
        /// Creates a new course.  
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CreateCourseResponse>> Create([FromBody] CreateCourseRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid or missing user identity." });

            try
            {
                var created = await _courseService.CreateCourseAsync(request, userId, ct);

                return Ok(created);
                //return CreatedAtRoute("GetCourseById", new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/course
        /// Updates an existing course.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<UpdateCourseResponse>> Update(
            [FromRoute] string id,
            [FromBody] UpdateCourseRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                if (id != request.Id)
                {
                    _logger.LogWarning("Update course: mismatched IDs. Route ID: {RouteId}, Body ID: {BodyId}", id, request.Id);
                    return BadRequest(new { message = "The ID in the URL must match the ID in the request body" });
                }
                var updated = await _courseService.UpdateCourseAsync(request, ct);
                return Ok(updated);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// PATCH /api/course/{id}/join
        /// Join course with given id.
        /// </summary>
        [HttpPatch("{id}/join")]
        public async Task<IActionResult> JoinCourse(String id, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);

                await _courseService.AddUserToCourseAsync(id, userId, ct);

                return NoContent();
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/work")]
        public async Task<ActionResult<TopicDTO>> GetWorksOfCourseAndUser(String id,
            [FromQuery] string? type,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            CancellationToken ct = default)
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);

            var result = await _courseService.GetAllWorksOfCourseAndUserAsync(id, userId, type, start, end, ct);

            return Ok(result);
        }

        [HttpGet("{courseId}/assignment-report")]
        [ProducesResponseType(typeof(AllAssignmentsReportDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<AllAssignmentsReportDTO>> GetAllAssignmentReport(
            [FromRoute] String courseId,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            CancellationToken ct)
        {
            var report = await _courseService.GetAssignmentsReportAsync(courseId, start, end, ct);

            return Ok(report);
        }

        [HttpGet("{courseId}/quiz-report")]
        public async Task<ActionResult<AllQuizzesReportDTO>> GetAllQuizzesReport(
            [FromRoute] String courseId,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            CancellationToken ct)
        {
            var report = await _courseService.GetQuizzesReportAsync(courseId, start, end, ct);

            return Ok(report);
        }

        [HttpPost("{sourceCourseId}/clone")]
        public async Task<ActionResult<CloneCourseResponse>> CloneCourse(
            [FromRoute] string sourceCourseId,
            [FromBody] CloneCourseRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid or missing user identity." });

            try
            {
                var result = await _courseCloneService.CloneAsync(sourceCourseId, request, userId, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clone course failed. sourceCourseId={SourceCourseId}, newCourseId={NewCourseId}",
                    sourceCourseId, request.NewCourseId);
                return BadRequest(new { message = "Failed to clone course" });
            }
        }
    }
}
