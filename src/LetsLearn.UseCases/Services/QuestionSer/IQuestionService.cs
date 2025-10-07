using LetsLearn.UseCases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.QuestionSer
{
    public interface IQuestionService
    {
        Task<GetQuestionResponse> CreateAsync(CreateQuestionRequest req, Guid userId, CancellationToken ct = default);
        Task<GetQuestionResponse> UpdateAsync(UpdateQuestionRequest req, Guid userId, CancellationToken ct = default);
        Task<GetQuestionResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<GetQuestionResponse>> GetByCourseIdAsync(String courseId, CancellationToken ct = default);
    }
}
