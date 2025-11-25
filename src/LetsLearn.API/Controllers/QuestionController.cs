using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace LetsLearn.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _service;

        public QuestionController(IQuestionService service)
        {
            _service = service;
        }

        // POST: question
        [HttpPost]
        public async Task<ActionResult<GetQuestionResponse>> Create(
            [FromBody] CreateQuestionRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid or missing user identity." });

            //request.ModifiedById = userId;

            try
            {
                var created = await _service.CreateAsync(request, userId, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // GET: question?courseId=CS01
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetQuestionResponse>>> GetQuestions(
            [FromQuery] string? courseId = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return BadRequest(new { message = "CourseId query parameter is required." });

            try
            {
                var list = await _service.GetByCourseIdAsync(courseId, ct);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // GET: api/question/{id}
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<GetQuestionResponse>> GetById(Guid id, CancellationToken ct)
        {
            try
            {
                var data = await _service.GetByIdAsync(id, ct);
                return Ok(data);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Question not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/question/by-course/{courseId}
        [HttpGet("by-course/{courseId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetQuestionResponse>>> GetByCourse(String courseId, CancellationToken ct)
        {
            try
            {
                var list = await _service.GetByCourseIdAsync(courseId, ct);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/question
        [HttpPut]
        public async Task<ActionResult<GetQuestionResponse>> Update(
            [FromBody] UpdateQuestionRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);
            if (userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid or missing user identity." });

            try
            {
                var updated = await _service.UpdateAsync(request, userId, ct);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Question not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private Guid GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }
    }
}
