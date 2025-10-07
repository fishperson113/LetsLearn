using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.QuestionSer
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _uow;

        public QuestionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<GetQuestionResponse> CreateAsync(CreateQuestionRequest req, Guid userId, CancellationToken ct = default)
        {
            var question = new Question
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeletedAt = null,

                QuestionName = req.QuestionName,
                QuestionText = req.QuestionText,
                Status = req.Status,
                Type = req.Type,
                DefaultMark = (decimal?)req.DefaultMark,
                Usage = req.Usage,
                FeedbackOfTrue = req.FeedbackOfTrue,
                FeedbackOfFalse = req.FeedbackOfFalse,
                CorrectAnswer = req.CorrectAnswer ?? false,
                Multiple = req.Multiple,

                CreatedById = userId,
                ModifiedById = userId,

                CourseId = req.CourseId ?? ""
            };

            await _uow.Questions.AddAsync(question);
            await _uow.CommitAsync();

            if (req.Choices is { Count: > 0 })
            {
                var choicesToAdd = req.Choices.Select(c => new QuestionChoice
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Text = c.Content,
                    Feedback = c.Feedback,
                    GradePercent = c.IsCorrect ? 100 : 0
                }).ToList();

                if (choicesToAdd.Count > 0)
                {
                    await _uow.QuestionChoices.AddRangeAsync(choicesToAdd);
                    await _uow.CommitAsync();
                }
            }

            var created = await _uow.Questions.GetWithChoicesAsync(question.Id, ct)
                          ?? throw new Exception("Failed to reload created question.");
            return MapToResponse(created);
        }

        public async Task<GetQuestionResponse> UpdateAsync(UpdateQuestionRequest req, Guid userId, CancellationToken ct = default)
        {

            var question = await _uow.Questions.GetWithChoicesAsync(req.Id, ct)
                          ?? throw new KeyNotFoundException("Question not found.");

            question.QuestionName = req.QuestionName;
            question.QuestionText = req.QuestionText;
            question.Status = req.Status;
            question.Type = req.Type;
            question.DefaultMark = (decimal?)req.DefaultMark;
            question.Usage = req.Usage;
            question.FeedbackOfTrue = req.FeedbackOfTrue;
            question.FeedbackOfFalse = req.FeedbackOfFalse;
            if (req.CorrectAnswer.HasValue) question.CorrectAnswer = req.CorrectAnswer.Value;
            if (!string.IsNullOrWhiteSpace(req.CourseId)) question.CourseId = req.CourseId;
            question.ModifiedById = userId;
            question.UpdatedAt = DateTime.UtcNow;

            if (req.Choices is { Count: > 0 })
            {
                var existingById = question.Choices.ToDictionary(x => x.Id, x => x);

                foreach (var c in req.Choices.Where(x => existingById.ContainsKey(x.Id)))
                {
                    var ex = existingById[c.Id];
                    ex.Text = c.Content;
                    ex.Feedback = c.Feedback;
                    ex.GradePercent = c.IsCorrect ? 100 : 0;
                }

                var toAdd = req.Choices
                    .Where(x => !existingById.ContainsKey(x.Id))
                    .Select(x => new QuestionChoice
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = question.Id,
                        Text = x.Content,
                        Feedback = x.Feedback,
                        GradePercent = x.IsCorrect ? 100 : 0
                    })
                    .ToList();

                if (toAdd.Count > 0)
                    await _uow.QuestionChoices.AddRangeAsync(toAdd);
            }

            await _uow.CommitAsync();

            var updated = await _uow.Questions.GetWithChoicesAsync(req.Id, ct)
                         ?? throw new Exception("Failed to reload updated question.");
            return MapToResponse(updated);
        }

        public async Task<GetQuestionResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var q = await _uow.Questions.GetWithChoicesAsync(id, ct)
                    ?? throw new KeyNotFoundException("Question not found.");
            return MapToResponse(q);
        }

        public async Task<List<GetQuestionResponse>> GetByCourseIdAsync(string courseId, CancellationToken ct = default)
        {
            var questions = await _uow.Questions.GetAllByCourseIdAsync(courseId, ct);
            return questions.Select(MapToResponse).ToList();
        }

        private static GetQuestionResponse MapToResponse(Question q)
        {
            return new GetQuestionResponse
            {
                Id = q.Id,
                CourseId = q.CourseId,
                QuestionName = q.QuestionName,
                QuestionText = q.QuestionText,
                Status = q.Status,
                Type = q.Type,
                DefaultMark = (double?)q.DefaultMark,
                Usage = q.Usage,
                FeedbackOfTrue = q.FeedbackOfTrue,
                FeedbackOfFalse = q.FeedbackOfFalse,
                CorrectAnswer = q.CorrectAnswer,
                Multiple = q.Multiple,
                CreatedById = q.CreatedById,
                ModifiedById = q.ModifiedById,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                Choices = q.Choices?.Select(c => new GetQuestionChoiceResponse
                {
                    Id = c.Id,
                    QuestionId = c.QuestionId,
                    Content = c.Text,
                    Feedback = c.Feedback,
                    IsCorrect = (c.GradePercent ?? 0) == 100
                }).ToList()
            };
        }
    }
}