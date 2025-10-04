using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;           
using LetsLearn.UseCases.Services.CourseSer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ICourseService courseService, ILogger<CourseController> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/course?userId=...
        /// Retrieves a list of courses.
        /// - If <paramref name="userId"/> is provided, returns courses created by that user.  
        /// - If no userId is provided, returns all published (public) courses.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<CourseResponse>>> Get([FromQuery] Guid? userId, CancellationToken ct)
        {
            try
            {
                if (userId.HasValue && userId.Value != Guid.Empty)
                {
                    var byCreator = await _courseService.GetAllByUserIdAsync(userId.Value, ct);
                    return Ok(byCreator);
                }

                var publics = await _courseService.GetAllPublicAsync(ct);
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
        public async Task<ActionResult<CourseResponse>> GetById(String id, CancellationToken ct)
        {
            try
            {
                var course = await _courseService.GetByIdAsync(id, ct);
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
        public async Task<ActionResult<CourseResponse>> Create([FromBody] CourseRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (request.CreatorId == Guid.Empty)
                return Unauthorized(new { message = "Invalid or missing user identity." });

            try
            {
                var created = await _courseService.CreateAsync(request, ct);

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
        /// PUT /api/course/{id}
        /// Updates an existing course.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CourseResponse>> Update(String id, [FromBody] CourseRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid or missing user identity." });

            try
            {
                var updated = await _courseService.UpdateAsync(id, request, ct);
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
    }

}
