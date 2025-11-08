using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.QuizResponseService
{
    public class QuizResponseService : IQuizResponseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuizResponseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private QuizResponseDTO ToDto(QuizResponse entity)
        {
            return new QuizResponseDTO
            {
                Id = entity.Id,
                TopicId = entity.TopicId,
                StudentId = entity.StudentId,
                Data = new QuizResponseData
                {
                    Status = entity.Status,
                    StartedAt = entity.StartedAt,
                    CompletedAt = entity.CompletedAt,
                    Answers = entity.Answers.Select(a => new QuizResponseAnswerDTO
                    {
                        TopicQuizQuestion = JsonSerializer.Deserialize<Question>(a.Question!)!,
                        Answer = a.Answer,
                        Mark = a.Mark
                    }).ToList()
                }
            };
        }

        public async Task<QuizResponseDTO> CreateQuizResponseAsync(QuizResponseRequest dto, Guid studentId, CancellationToken ct = default)
        {
            var entity = new QuizResponse
            {
                Id = Guid.NewGuid(),
                TopicId = dto.TopicId,
                StudentId = studentId,
                Status = dto.Data.Status,
                StartedAt = dto.Data.StartedAt,
                CompletedAt = dto.Data.CompletedAt,
                Answers = new List<QuizResponseAnswer>()
            };

            foreach (var a in dto.Data.Answers)
            {
                entity.Answers.Add(new QuizResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuizResponseId = entity.Id,
                    Question = JsonSerializer.Serialize(a.TopicQuizQuestion),
                    Answer = a.Answer,
                    Mark = a.Mark
                });
            }

            await _unitOfWork.QuizResponses.AddAsync(entity);
            await _unitOfWork.CommitAsync();
            return ToDto(entity);
        }
        public async Task<QuizResponseDTO> UpdateQuizResponseByIdAsync(Guid id, QuizResponseRequest dto, CancellationToken ct = default)
        {
            var entity = await _unitOfWork.QuizResponses.GetByIdTrackedWithAnswersAsync(id,ct);
            if (entity == null)
                throw new Exception("Quiz response not found");

            entity.Status = dto.Data.Status;
            entity.StartedAt = dto.Data.StartedAt;
            entity.CompletedAt = dto.Data.CompletedAt;

            await _unitOfWork.QuizResponseAnswers.DeleteRangeAsync(entity.Answers);
            entity.Answers.Clear();

            foreach (var a in dto.Data.Answers)
            {
                entity.Answers.Add(new QuizResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuizResponseId = entity.Id,
                    Question = JsonSerializer.Serialize(a.TopicQuizQuestion),
                    Answer = a.Answer,
                    Mark = a.Mark
                });
            }

            await _unitOfWork.CommitAsync();
            return ToDto(entity);
        }
        public async Task<QuizResponseDTO> GetQuizResponseByIdAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _unitOfWork.QuizResponses.GetByIdTrackedWithAnswersAsync(id,ct);
            if (entity == null)
            {
                throw new Exception("Quiz response not found");
            }

            return ToDto(entity);
        }
        public async Task<List<QuizResponseDTO>> GetAllQuizResponsesByTopicIdAsync(Guid topicId, CancellationToken ct = default)
        {
            var entities = await _unitOfWork.QuizResponses.FindAllByTopicIdWithAnswersAsync(topicId, ct);
            return entities.Select(ToDto).ToList();
        }

        public async Task<List<QuizResponseDTO>> GetAllQuizResponsesByTopicIdOfStudentAsync(Guid topicId, Guid studentId, CancellationToken ct = default)
        {
            var entities = await _unitOfWork.QuizResponses.FindByTopicIdAndStudentIdWithAnswersAsync(topicId, studentId, ct);
            return entities.Select(ToDto).ToList();
        }

        //public async Task DeleteQuizResponseByIdAsync(Guid id)
        //{
        //    var entity = await _unitOfWork.QuizResponses.GetByIdAsync(id);
        //    if (entity == null) throw new Exception("Quiz response not found");

        //    await _unitOfWork.QuizResponses.DeleteAsync(entity);
        //    await _unitOfWork.CommitAsync();
        //}
    }
}
