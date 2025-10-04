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
        Task<QuestionResponse> CreateAsync(QuestionRequest req, Guid userId, CancellationToken ct = default);
        Task<QuestionResponse> UpdateAsync(Guid id, QuestionRequest req, Guid userId, CancellationToken ct = default);
        Task<QuestionResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<QuestionResponse>> GetByCourseIdAsync(String courseId, CancellationToken ct = default);
    }
}
