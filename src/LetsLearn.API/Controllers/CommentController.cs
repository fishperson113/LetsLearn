using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("[controller]")]

    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost("addComment")]
        public async Task<ActionResult> AddComment([FromBody] CreateCommentRequest createcommentDTO, CancellationToken ct = default)
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);

            await _commentService.AddCommentAsync(userId, createcommentDTO, ct);
            return Ok();
        }

        [HttpGet("getComment/{topicId}")]
        public async Task<ActionResult<List<GetCommentResponse>>> GetComments([FromRoute] Guid topicId, CancellationToken ct = default)
        {
            var comments = await _commentService.GetCommentsByTopicAsync(topicId, ct);
            return Ok(comments);
        }

        [HttpDelete("deleteComment/{commentId}")]
        public async Task<ActionResult> DeleteComment([FromRoute] Guid commentId,CancellationToken ct = default)
        {
            await _commentService.DeleteCommentAsync(commentId, ct);
            return NoContent();
        }
    }
}
