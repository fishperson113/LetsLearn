using LetsLearn.UseCases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.ServiceInterfaces
{
    public interface IAssignmentResponseService
    {
        Task<AssignmentResponseDTO> GetByIdAsync(Guid id);
        Task<AssignmentResponseDTO> CreateAsync(CreateAssignmentResponseRequest dto, Guid studentId);
        Task<IEnumerable<AssignmentResponseDTO>> GetAllByTopicIdAsync(Guid topicId);
        Task<AssignmentResponseDTO> UpdateByIdAsync(Guid id, UpdateAssignmentResponseRequest dto);
        Task DeleteAsync(Guid id);
    }
}
