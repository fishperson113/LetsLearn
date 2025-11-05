using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LetsLearn.API.Controllers
{
    [ApiController]
    [Route("topic/[controller]")]
    [Authorize]
    public class AssignmentResponseController : ControllerBase
    {
        private readonly IAssignmentResponseService _assignmentResponseService;

        public AssignmentResponseController(IAssignmentResponseService assignmentResponseService)
        {
            _assignmentResponseService = assignmentResponseService;
        }

        [HttpPost]
        public async Task<ActionResult<AssignmentResponseDTO>> CreateAssignmentResponse([FromBody] CreateAssignmentResponseRequest dto)
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "userID").Value);

            var result = await _assignmentResponseService.CreateAsync(dto, userId);
            return Ok(result);
        }

        [HttpGet("getAll/{topicId}")]
        public async Task<ActionResult<IEnumerable<AssignmentResponseDTO>>> GetAllAssignmentResponsesByTopicId([FromRoute] Guid topicId)
        {
            var res = await _assignmentResponseService.GetAllByTopicIdAsync(topicId);
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AssignmentResponseDTO>> GetAssignmentResponseById([FromRoute] Guid id)
        {
            var res = await _assignmentResponseService.GetByIdAsync(id);
            return Ok(res);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AssignmentResponseDTO>> UpdateAssignmentResponseById([FromRoute] Guid id, [FromBody] UpdateAssignmentResponseRequest dto)
        {
            var result = await _assignmentResponseService.UpdateByIdAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAssignmentResponseById([FromRoute] Guid id)
        {
            await _assignmentResponseService.DeleteAsync(id);
            return NoContent();
        }
    }
}
