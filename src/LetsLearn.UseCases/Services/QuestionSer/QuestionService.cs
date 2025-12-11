using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<QuestionService> _logger;

        public QuestionService(IUnitOfWork uow,ILogger<QuestionService>logger)
        {
            _uow = uow;
            _logger = logger;
        }

        // Test Case Estimation:
        // Decision points (D):
        // - if Choices provided (req.Choices Count > 0): +1
        // - if choicesToAdd.Count > 0: +1
        // - Null-coalesce throw on reload created question: +1
        // D = 3 => Minimum Test Cases = D + 1 = 4
        public async Task<GetQuestionResponse> CreateAsync(CreateQuestionRequest req, Guid userId, CancellationToken ct = default)
        {
            var courseId = req.CourseId ?? req.Course?.Id ?? string.Empty;
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

                CourseId = courseId
            };

            await _uow.Questions.AddAsync(question);

            if (req.Choices is { Count: > 0 })
            {
                var choicesToAdd = req.Choices.Select(c => new QuestionChoice
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Text = c.Text,
                    Feedback = c.Feedback,
                    GradePercent = c.GradePercent
                }).ToList();

                    if (choicesToAdd.Count > 0)
                    {
                        await _uow.QuestionChoices.AddRangeAsync(choicesToAdd);
                        try
                        {
                            await _uow.CommitAsync();
                        }
                        catch (DbUpdateException ex)
                        {
                            throw new InvalidOperationException("Failed to save question choices.", ex);
                        }
                    }
            }

            try
            {
                await _uow.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to create question.", ex);
            }

            var created = await _uow.Questions.GetWithChoicesAsync(question.Id, ct)
                          ?? throw new InvalidOperationException("Failed to reload created question.");
            return MapToResponse(created);
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Null-coalesce throw when question not found: +1
        // - if CorrectAnswer.HasValue: +1
        // - if CourseId provided: +1
        // - if Choices provided (req.Choices Count > 0): +1
        // - if toAdd.Count > 0: +1
        // - Null-coalesce throw on reload updated question: +1
        // D = 6 => Minimum Test Cases = D + 1 = 7
        public async Task<GetQuestionResponse> UpdateAsync(UpdateQuestionRequest req, Guid userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting update for question {QuestionId} by user {UserId}", req.Id, userId);

            var question = await _uow.Questions.GetWithChoicesAsync(req.Id, ct);
            if (question == null)
            {
                _logger.LogWarning("Question {QuestionId} not found for update", req.Id);
                throw new KeyNotFoundException("Question not found.");
            }

            _logger.LogDebug("Question {QuestionId} found. Current state: CourseId={CourseId}, DeletedAt={DeletedAt}, ChoiceCount={ChoiceCount}",
                question.Id, question.CourseId, question.DeletedAt, question.Choices?.Count ?? 0);

            // Store original values for logging
            var originalCourseId = question.CourseId;
            var originalDeletedAt = question.DeletedAt;
            var originalChoiceCount = question.Choices?.Count ?? 0;

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
            // Make sure DeletedAt remains null
            question.DeletedAt = null;

            _logger.LogDebug("Question {QuestionId} updated. CourseId: {OriginalCourseId} -> {NewCourseId}, DeletedAt: {OriginalDeletedAt} -> {NewDeletedAt}",
                question.Id, originalCourseId, question.CourseId, originalDeletedAt, question.DeletedAt);

            // Handle choices updates
            if (req.Choices is { Count: > 0 })
            {
                _logger.LogInformation("Processing {RequestChoiceCount} choices for question {QuestionId} (currently has {ExistingChoiceCount})",
                    req.Choices.Count, question.Id, originalChoiceCount);

                var existingById = question.Choices.ToDictionary(x => x.Id, x => x);
                var incomingIds = req.Choices.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();

                _logger.LogDebug("Question {QuestionId}: Existing choice IDs: [{ExistingIds}], Incoming choice IDs: [{IncomingIds}]",
                    question.Id,
                    string.Join(", ", existingById.Keys),
                    string.Join(", ", incomingIds));

                // Mark choices for deletion (don't delete immediately)
                var choicesToRemove = question.Choices.Where(c => !incomingIds.Contains(c.Id)).ToList();
                if (choicesToRemove.Any())
                {
                    _logger.LogInformation("Removing {RemoveCount} choices from question {QuestionId}: [{RemoveIds}]",
                        choicesToRemove.Count, question.Id, string.Join(", ", choicesToRemove.Select(c => c.Id)));

                    await _uow.QuestionChoices.DeleteRangeAsync(choicesToRemove);
                }

                // Update existing choices
                var choicesToUpdate = req.Choices.Where(x => x.Id.HasValue && existingById.ContainsKey(x.Id.Value)).ToList();
                if (choicesToUpdate.Any())
                {
                    _logger.LogInformation("Updating {UpdateCount} existing choices for question {QuestionId}",
                        choicesToUpdate.Count, question.Id);

                    foreach (var c in choicesToUpdate)
                    {
                        var ex = existingById[c.Id!.Value];
                        ex.Text = c.Text;
                        ex.Feedback = c.Feedback;
                        ex.GradePercent = c.GradePercent;
                        _logger.LogDebug("Updated choice {ChoiceId} for question {QuestionId}", ex.Id, question.Id);
                    }
                }

                // Add new choices
                var toAdd = req.Choices
                    .Where(x => !x.Id.HasValue || !existingById.ContainsKey(x.Id.Value))
                    .Select(x => new QuestionChoice
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = question.Id,
                        Text = x.Text,
                        Feedback = x.Feedback,
                        GradePercent = x.GradePercent
                    })
                    .ToList();

                if (toAdd.Count > 0)
                {
                    _logger.LogInformation("Adding {AddCount} new choices to question {QuestionId}: [{AddIds}]",
                        toAdd.Count, question.Id, string.Join(", ", toAdd.Select(c => c.Id)));

                    await _uow.QuestionChoices.AddRangeAsync(toAdd);
                }
            }
            else
            {
                // If no choices provided, remove all existing choices
                if (question.Choices.Any())
                {
                    _logger.LogInformation("No choices provided, removing all {ExistingCount} choices from question {QuestionId}",
                        question.Choices.Count, question.Id);

                    await _uow.QuestionChoices.DeleteRangeAsync(question.Choices);
                }
            }

            try
            {
                _logger.LogDebug("Committing changes for question {QuestionId}", question.Id);
                await _uow.CommitAsync();
                _logger.LogInformation("Question {QuestionId} updated successfully", question.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update question {QuestionId}. CourseId={CourseId}, DeletedAt={DeletedAt}",
                    question.Id, question.CourseId, question.DeletedAt);
                throw new InvalidOperationException("Failed to update question.", ex);
            }

            // Verify the question is still findable
            _logger.LogDebug("Reloading question {QuestionId} to verify update", question.Id);
            var updated = await _uow.Questions.GetWithChoicesAsync(req.Id, ct);

            if (updated == null)
            {
                _logger.LogError("Question {QuestionId} not found after update! This indicates the question may have been marked as deleted.", req.Id);

                // Try to find the question without the DeletedAt filter to see if it exists but is marked deleted
                var questionInDb = await _uow.Questions.FirstOrDefaultAsync(q => q.Id == req.Id, ct);
                if (questionInDb != null)
                {
                    _logger.LogError("Question {QuestionId} exists but is marked as deleted. DeletedAt={DeletedAt}, CourseId={CourseId}",
                        questionInDb.Id, questionInDb.DeletedAt, questionInDb.CourseId);
                }
                else
                {
                    _logger.LogError("Question {QuestionId} completely missing from database", req.Id);
                }

                throw new InvalidOperationException("Failed to reload updated question.");
            }

            _logger.LogInformation("Question {QuestionId} reloaded successfully after update. CourseId={CourseId}, DeletedAt={DeletedAt}, ChoiceCount={ChoiceCount}",
                updated.Id, updated.CourseId, updated.DeletedAt, updated.Choices?.Count ?? 0);

            return MapToResponse(updated);
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Null-coalesce throw when question not found: +1
        // D = 1 => Minimum Test Cases = D + 1 = 2
        public async Task<GetQuestionResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var q = await _uow.Questions.GetWithChoicesAsync(id, ct)
                    ?? throw new KeyNotFoundException("Question not found.");
            return MapToResponse(q);
        }

        // Test Case Estimation:
        // Decision points (D):
        // - No branching here: +0
        // D = 0 => Minimum Test Cases = D + 1 = 1
        public async Task<IEnumerable<GetQuestionResponse>> GetByCourseIdAsync(string courseId, CancellationToken ct = default)
        {
            var questions = await _uow.Questions.GetAllByCourseIdAsync(courseId, ct);
            return questions.Select(MapToResponse).ToList();
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Pure mapping, no branching: +0
        // D = 0 => Minimum Test Cases = D + 1 = 1 (basic happy-path mapping)
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
                    Text = c.Text,
                    Feedback = c.Feedback,
                    GradePercent = c.GradePercent
                }).ToList()
            };
        }
    }
}
