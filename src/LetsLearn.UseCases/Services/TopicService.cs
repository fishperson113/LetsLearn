using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services
{
    public class TopicService : ITopicService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TopicService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TopicService(IUnitOfWork unitOfWork, ILogger<TopicService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TopicResponse> CreateTopicAsync(CreateTopicRequest request, CancellationToken ct = default)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                _logger.LogInformation("Creating topic of type {Type} for section {SectionId}", request.Type, request.SectionId);

                // Bước 1: Tạo Topic cha
                var topic = new Topic
                {
                    Title = request.Title,
                    Type = request.Type,
                    SectionId = request.SectionId
                };

                await _unitOfWork.Topics.AddAsync(topic);

                object? topicData = null;

                // Bước 2: Xử lý từng kiểu cụ thể
                //switch (request)
                //{
                //    case CreateTopicPageRequest pageReq:
                //        var page = new TopicPage
                //        {
                //            TopicId = topic.Id,
                //            Description = pageReq.Description,
                //            Content = pageReq.Content
                //        };
                //        await _unitOfWork.TopicPages.AddAsync(page);
                //        topicData = page;
                //        break;

                //    case CreateTopicFileRequest fileReq:
                //        var file = new TopicFile
                //        {
                //            TopicId = topic.Id,
                //            Description = fileReq.Description
                //        };
                //        await _unitOfWork.TopicFiles.AddAsync(file);
                //        topicData = file;
                //        break;

                //    case CreateTopicLinkRequest linkReq:
                //        var link = new TopicLink
                //        {
                //            TopicId = topic.Id,
                //            Description = linkReq.Description,
                //            Url = linkReq.Url
                //        };
                //        await _unitOfWork.TopicLinks.AddAsync(link);
                //        topicData = link;
                //        break;

                //    case CreateTopicQuizRequest quizReq:
                //        var quiz = new TopicQuiz
                //        {
                //            TopicId = topic.Id,
                //            Description = quizReq.Description,
                //            Open = quizReq.Open,
                //            Close = quizReq.Close,
                //            TimeLimit = quizReq.TimeLimit,
                //            TimeLimitUnit = quizReq.TimeLimitUnit,
                //            GradeToPass = quizReq.GradeToPass,
                //            GradingMethod = quizReq.GradingMethod,
                //            AttemptAllowed = quizReq.AttemptAllowed,
                //            Questions = quizReq.Questions.Select(q => new TopicQuizQuestion
                //            {
                //                QuestionName = q.QuestionName,
                //                QuestionText = q.QuestionText,
                //                Type = q.Type,
                //                DefaultMark = q.DefaultMark,
                //                FeedbackOfTrue = q.FeedbackOfTrue,
                //                FeedbackOfFalse = q.FeedbackOfFalse,
                //                CorrectAnswer = q.CorrectAnswer,
                //                Multiple = q.Multiple,
                //                Choices = q.Choices.Select(c => new TopicQuizQuestionChoice
                //                {
                //                    Text = c.Text,
                //                    GradePercent = c.GradePercent,
                //                    Feedback = c.Feedback
                //                }).ToList()
                //            }).ToList()
                //        };
                //        await _unitOfWork.TopicQuizzes.AddAsync(quiz);
                //        topicData = quiz;
                //        break;

                //    default:
                //        throw new NotSupportedException($"Unsupported topic type: {request.Type}");
                //}

                // Bước 2: Deserialize Data và xử lý theo Type
                switch (request.Type.ToLower())
                {
                    case "page":
                        {
                            var pageReq = JsonSerializer.Deserialize<CreateTopicPageRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            var page = new TopicPage
                            {
                                TopicId = topic.Id,
                                Description = pageReq?.Description,
                                Content = pageReq?.Content
                            };

                            await _unitOfWork.TopicPages.AddAsync(page);
                            topicData = page;
                            break;
                        }

                    case "file":
                        {
                            var fileReq = JsonSerializer.Deserialize<CreateTopicFileRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            var file = new TopicFile
                            {
                                TopicId = topic.Id,
                                Description = fileReq?.Description,
                            };

                            await _unitOfWork.TopicFiles.AddAsync(file);
                            topicData = file;
                            break;
                        }

                    case "link":
                        {
                            var linkReq = JsonSerializer.Deserialize<CreateTopicLinkRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            var link = new TopicLink
                            {
                                TopicId = topic.Id,
                                Description = linkReq?.Description,
                                Url = linkReq?.Url
                            };

                            await _unitOfWork.TopicLinks.AddAsync(link);
                            topicData = link;
                            break;
                        }

                    case "assignment":
                        {
                            var assignReq = JsonSerializer.Deserialize<CreateTopicAssignmentRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            var assignment = new TopicAssignment
                            {
                                TopicId = topic.Id,
                                Description = assignReq?.Description,
                                Open = assignReq?.Open,
                                Close = assignReq?.Close,
                                MaximumFile = assignReq?.MaximumFile,
                                MaximumFileSize = assignReq?.MaximumFileSize,
                                RemindToGrade = assignReq?.RemindToGrade
                            };

                            await _unitOfWork.TopicAssignments.AddAsync(assignment);
                            topicData = assignment;
                            break;
                        }

                    case "quiz":
                        {
                            var quizReq = JsonSerializer.Deserialize<CreateTopicQuizRequest>(request.Data!.Value.GetRawText(),new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            var quiz = new TopicQuiz
                            {
                                TopicId = topic.Id,
                                Description = quizReq?.Description,
                                Open = quizReq?.Open,
                                Close = quizReq?.Close,
                                TimeLimit = quizReq?.TimeLimit,
                                TimeLimitUnit = quizReq?.TimeLimitUnit,
                                GradeToPass = quizReq?.GradeToPass,
                                GradingMethod = quizReq?.GradingMethod,
                                AttemptAllowed = quizReq?.AttemptAllowed,
                                Questions = quizReq?.Questions?.Select(q => new TopicQuizQuestion
                                {
                                    QuestionName = q.QuestionName,
                                    QuestionText = q.QuestionText,
                                    Type = q.Type,
                                    DefaultMark = q.DefaultMark,
                                    FeedbackOfTrue = q.FeedbackOfTrue,
                                    FeedbackOfFalse = q.FeedbackOfFalse,
                                    CorrectAnswer = q.CorrectAnswer,
                                    Multiple = q.Multiple,
                                    Choices = q.Choices.Select(c => new TopicQuizQuestionChoice
                                    {
                                        Text = c.Text,
                                        GradePercent = c.GradePercent,
                                        Feedback = c.Feedback
                                    }).ToList()
                                }).ToList()
                            };

                            await _unitOfWork.TopicQuizzes.AddAsync(quiz);
                            topicData = quiz;
                            break;
                        }

                    default:
                        _logger.LogWarning("Unsupported topic type: {Type}", request.Type);
                        throw new NotSupportedException($"Unsupported topic type: {request.Type}");
                }

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Successfully created topic {TopicId} ({Type})", topic.Id, request.Type);

                return new TopicResponse
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    Type = topic.Type,
                    SectionId = topic.SectionId,
                    Data = topicData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating topic of type {Type}", request?.Type);
                throw;
            }
        }

        public async Task<TopicResponse> UpdateTopicAsync(UpdateTopicRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Updating topic {TopicId}", request.Id);

            var topic = await _unitOfWork.Topics.GetByIdAsync(request.Id!.Value);
            if (topic == null)
            {
                _logger.LogWarning("Topic {TopicId} not found.", request.Id);
                throw new KeyNotFoundException("Topic not found.");
            }

            // Cập nhật thông tin chung
            topic.Title = request.Title ?? topic.Title;
            await _unitOfWork.Topics.UpdateAsync(topic);

            object? topicData = null;

            //// Xử lý cập nhật chi tiết theo từng kiểu
            //switch (request)
            //{
            //    case UpdateTopicPageRequest pageReq:
            //        var page = (await _unitOfWork.TopicPages.FindAsync(p => p.TopicId == topic.Id)).FirstOrDefault();
            //        if (page == null)
            //        {
            //            _logger.LogWarning("TopicPage not found for topic {TopicId}", topic.Id);
            //            throw new KeyNotFoundException("TopicPage not found.");
            //        }

            //        page.Description = pageReq.Description ?? page.Description;
            //        page.Content = pageReq.Content ?? page.Content;
            //        await _unitOfWork.TopicPages.UpdateAsync(page);
            //        topicData = page;
            //        break;

            //    case UpdateTopicFileRequest fileReq:
            //        var file = (await _unitOfWork.TopicFiles.FindAsync(f => f.TopicId == topic.Id, ct)).FirstOrDefault();
            //        if (file == null)
            //        {
            //            _logger.LogWarning("TopicFile not found for topic {TopicId}", topic.Id);
            //            throw new KeyNotFoundException("TopicFile not found.");
            //        }

            //        file.Description = fileReq.Description ?? file.Description;
            //        await _unitOfWork.TopicFiles.UpdateAsync(file);
            //        topicData = file;
            //        break;


            //    case UpdateTopicLinkRequest linkReq:
            //        var link = (await _unitOfWork.TopicLinks.FindAsync(l => l.TopicId == topic.Id, ct)).FirstOrDefault();
            //        if (link == null)
            //        {
            //            _logger.LogWarning("TopicLink not found for topic {TopicId}", topic.Id);
            //            throw new KeyNotFoundException("TopicLink not found.");
            //        }

            //        link.Description = linkReq.Description ?? link.Description;
            //        link.Url = linkReq.Url ?? link.Url;
            //        await _unitOfWork.TopicLinks.UpdateAsync(link);
            //        topicData = link;
            //        break;

            //    case UpdateTopicQuizRequest quizReq:
            //        var quiz = (await _unitOfWork.TopicQuizzes.FindAsync(q => q.TopicId == topic.Id, ct)).FirstOrDefault();
            //        if (quiz == null)
            //        {
            //            _logger.LogWarning("TopicQuiz not found for topic {TopicId}", topic.Id);
            //            throw new KeyNotFoundException("TopicQuiz not found.");
            //        }

            //        quiz.Description = quizReq.Description ?? quiz.Description;
            //        quiz.Open = quizReq.Open ?? quiz.Open;
            //        quiz.Close = quizReq.Close ?? quiz.Close;
            //        quiz.TimeLimit = quizReq.TimeLimit ?? quiz.TimeLimit;
            //        quiz.TimeLimitUnit = quizReq.TimeLimitUnit ?? quiz.TimeLimitUnit;
            //        quiz.GradeToPass = quizReq.GradeToPass ?? quiz.GradeToPass;
            //        quiz.GradingMethod = quizReq.GradingMethod ?? quiz.GradingMethod;
            //        quiz.AttemptAllowed = quizReq.AttemptAllowed ?? quiz.AttemptAllowed;

            //        if (quizReq.Questions != null && quizReq.Questions.Any())
            //        {
            //            quiz.Questions = quizReq.Questions.Select(q => new TopicQuizQuestion
            //            {
            //                QuestionName = q.QuestionName,
            //                QuestionText = q.QuestionText,
            //                Type = q.Type,
            //                DefaultMark = q.DefaultMark,
            //                FeedbackOfTrue = q.FeedbackOfTrue,
            //                FeedbackOfFalse = q.FeedbackOfFalse,
            //                CorrectAnswer = q.CorrectAnswer,
            //                Multiple = q.Multiple,
            //                Choices = q.Choices.Select(c => new TopicQuizQuestionChoice
            //                {
            //                    Text = c.Text,
            //                    GradePercent = c.GradePercent,
            //                    Feedback = c.Feedback
            //                }).ToList()
            //            }).ToList();
            //        }

            //        await _unitOfWork.TopicQuizzes.UpdateAsync(quiz);
            //        topicData = quiz;
            //        break;

            //    default:
            //        _logger.LogError("Unsupported topic type for update: {Type}", request.GetType().Name);
            //        throw new NotSupportedException($"Unsupported topic type: {request.GetType().Name}");
            //}

            // Chuyển đổi dựa vào type do người dùng truyền
            switch (request.Type?.ToLower())
            {
                case "page":
                    {
                        var pageReq = JsonSerializer.Deserialize<UpdateTopicPageRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var page = (await _unitOfWork.TopicPages.FindAsync(p => p.TopicId == topic.Id)).FirstOrDefault()
                                   ?? throw new KeyNotFoundException("TopicPage not found.");
                        page.Description = pageReq.Description ?? page.Description;
                        page.Content = pageReq.Content ?? page.Content;
                        await _unitOfWork.TopicPages.UpdateAsync(page);
                        topicData = page;
                        break;
                    }

                case "file":
                    {
                        var fileReq = JsonSerializer.Deserialize<UpdateTopicFileRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var file = (await _unitOfWork.TopicFiles.FindAsync(f => f.TopicId == topic.Id, ct)).FirstOrDefault()
                                   ?? throw new KeyNotFoundException("TopicFile not found.");
                        file.Description = fileReq.Description ?? file.Description;
                        await _unitOfWork.TopicFiles.UpdateAsync(file);
                        topicData = file;
                        break;
                    }

                case "link":
                    {
                        var linkReq = JsonSerializer.Deserialize<UpdateTopicLinkRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var link = (await _unitOfWork.TopicLinks.FindAsync(l => l.TopicId == topic.Id, ct)).FirstOrDefault()
                                   ?? throw new KeyNotFoundException("TopicLink not found.");
                        link.Description = linkReq.Description ?? link.Description;
                        link.Url = linkReq.Url ?? link.Url;
                        await _unitOfWork.TopicLinks.UpdateAsync(link);
                        topicData = link;
                        break;
                    }

                case "assignment":
                    {
                        var assignmentReq = JsonSerializer.Deserialize<UpdateTopicAssignmentRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var assignment = (await _unitOfWork.TopicAssignments.FindAsync(a => a.TopicId == topic.Id, ct)).FirstOrDefault()
                                         ?? throw new KeyNotFoundException("TopicAssignment not found.");

                        assignment.Description = assignmentReq.Description ?? assignment.Description;
                        assignment.Open = assignmentReq.Open ?? assignment.Open;
                        assignment.Close = assignmentReq.Close ?? assignment.Close;
                        assignment.MaximumFile = assignmentReq.MaximumFile ?? assignment.MaximumFile;
                        assignment.MaximumFileSize = assignmentReq.MaximumFileSize ?? assignment.MaximumFileSize;
                        assignment.RemindToGrade = assignmentReq.RemindToGrade ?? assignment.RemindToGrade;

                        await _unitOfWork.TopicAssignments.UpdateAsync(assignment);
                        topicData = assignment;
                        break;
                    }

                case "quiz":
                    {
                        var quizReq = JsonSerializer.Deserialize<UpdateTopicQuizRequest>(request.Data!.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var quiz = await _unitOfWork.TopicQuizzes.GetWithQuestionsAsync(topic.Id)
                                   ?? throw new KeyNotFoundException("TopicQuiz not found.");

                        quiz.Description = quizReq.Description ?? quiz.Description;
                        quiz.Open = quizReq.Open ?? quiz.Open;
                        quiz.Close = quizReq.Close ?? quiz.Close;
                        quiz.TimeLimit = quizReq.TimeLimit ?? quiz.TimeLimit;
                        quiz.TimeLimitUnit = quizReq.TimeLimitUnit ?? quiz.TimeLimitUnit;
                        quiz.GradeToPass = quizReq.GradeToPass ?? quiz.GradeToPass;
                        quiz.GradingMethod = quizReq.GradingMethod ?? quiz.GradingMethod;
                        quiz.AttemptAllowed = quizReq.AttemptAllowed ?? quiz.AttemptAllowed;

                        if (quizReq.Questions != null && quizReq.Questions.Any())
                        {
                            var updatedQuestions = new List<TopicQuizQuestion>();

                            foreach (var q in quizReq.Questions)
                            {
                                TopicQuizQuestion question;

                                if (q.Id != Guid.Empty)
                                {
                                    // Update question hiện có
                                    question = quiz.Questions.FirstOrDefault(x => x.Id == q.Id)
                                               ?? throw new KeyNotFoundException($"Question with ID {q.Id} not found.");
                                    question.QuestionName = q.QuestionName;
                                    question.QuestionText = q.QuestionText;
                                    question.Type = q.Type;
                                    question.DefaultMark = q.DefaultMark;
                                    question.FeedbackOfTrue = q.FeedbackOfTrue;
                                    question.FeedbackOfFalse = q.FeedbackOfFalse;
                                    question.CorrectAnswer = q.CorrectAnswer;
                                    question.Multiple = q.Multiple;

                                    // Update choices
                                    var updatedChoices = new List<TopicQuizQuestionChoice>();
                                    foreach (var c in q.Choices)
                                    {
                                        TopicQuizQuestionChoice choice;
                                        if (c.Id != Guid.Empty)
                                        {
                                            choice = question.Choices.FirstOrDefault(x => x.Id == c.Id)
                                                     ?? throw new KeyNotFoundException($"Choice with ID {c.Id} not found.");
                                            choice.Text = c.Text;
                                            choice.GradePercent = c.GradePercent;
                                            choice.Feedback = c.Feedback;
                                        }
                                        else
                                        {
                                            // Tạo mới choice
                                            choice = new TopicQuizQuestionChoice
                                            {
                                                Id = Guid.NewGuid(),
                                                QuizQuestionId = question.Id,
                                                Text = c.Text,
                                                GradePercent = c.GradePercent,
                                                Feedback = c.Feedback
                                            };
                                        }
                                        updatedChoices.Add(choice);
                                    }

                                    question.Choices = updatedChoices;
                                }
                                else
                                {
                                    // Tạo mới question hoàn toàn
                                    question = new TopicQuizQuestion
                                    {
                                        Id = Guid.NewGuid(),
                                        TopicQuizId = request.Id!.Value,
                                        QuestionName = q.QuestionName,
                                        QuestionText = q.QuestionText,
                                        Type = q.Type,
                                        DefaultMark = q.DefaultMark,
                                        FeedbackOfTrue = q.FeedbackOfTrue,
                                        FeedbackOfFalse = q.FeedbackOfFalse,
                                        CorrectAnswer = q.CorrectAnswer,
                                        Multiple = q.Multiple,
                                        Choices = q.Choices.Select(c => new TopicQuizQuestionChoice
                                        {
                                            Id = Guid.NewGuid(),
                                            QuizQuestionId = Guid.Empty,
                                            Text = c.Text,
                                            GradePercent = c.GradePercent,
                                            Feedback = c.Feedback
                                        }).ToList()
                                    };
                                }

                                updatedQuestions.Add(question);
                            }

                            quiz.Questions = updatedQuestions;
                        }

                        await _unitOfWork.TopicQuizzes.UpdateAsync(quiz);
                        topicData = quiz;
                        break;
                    }

                default:
                    _logger.LogError("Unsupported topic type for update: {Type}", request.Type);
                    throw new NotSupportedException($"Unsupported topic type: {request.Type}");
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Successfully updated topic {TopicId}", topic.Id);

            return new TopicResponse
            {
                Id = topic.Id,
                Title = topic.Title,
                Type = topic.Type,
                SectionId = topic.SectionId,
                Data = topicData
            };
        }

        public async Task<bool> DeleteTopicAsync(Guid id, CancellationToken ct = default)
        {
            var topic = await _unitOfWork.Topics.GetByIdAsync(id);
            if (topic == null)
            {
                return false;
            }

            await _unitOfWork.Topics.DeleteAsync(topic);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<TopicResponse> GetTopicByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var topic = await _unitOfWork.Topics.GetByIdAsync(id);
                if (topic == null)
                {
                    _logger.LogWarning("Topic {TopicId} not found.", id);
                    throw new KeyNotFoundException("Topic not found.");
                }

                _logger.LogInformation("Fetching topic {TopicId} of type {Type}", id, topic.Type);

                object? topicData = null;

                switch (topic.Type.ToLower())
                {
                    case "quiz":
                        var quiz = await _unitOfWork.TopicQuizzes.GetWithQuestionsAsync(topic.Id);
                        if (quiz != null)
                            topicData = quiz;
                        break;

                    case "assignment":
                        var assignment = (await _unitOfWork.TopicAssignments.FindAsync(a => a.TopicId == topic.Id, ct)).FirstOrDefault();
                        if (assignment != null)
                            topicData = assignment;
                        break;

                    case "file":
                        var file = (await _unitOfWork.TopicFiles.FindAsync(f => f.TopicId == topic.Id, ct)).FirstOrDefault();
                        if (file != null)
                            topicData = file;
                        break;

                    case "link":
                        var link = (await _unitOfWork.TopicLinks.FindAsync(l => l.TopicId == topic.Id, ct)).FirstOrDefault();
                        if (link != null)
                            topicData = link;
                        break;

                    case "page":
                        var page = (await _unitOfWork.TopicPages.FindAsync(p => p.TopicId == topic.Id, ct)).FirstOrDefault();
                        if (page != null)
                            topicData = page;
                        break;

                    default:
                        _logger.LogWarning("Invalid topic type {Type} for topic {TopicId}", topic.Type, id);
                        throw new NotSupportedException($"Invalid topic type: {topic.Type}");
                }

                _logger.LogInformation("Successfully fetched topic {TopicId}", id);

                return new TopicResponse
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    Type = topic.Type,
                    SectionId = topic.SectionId,
                    Data = topicData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching topic {TopicId}", id);
                throw new Exception("Error fetching topic.", ex);
            }
        }

    //    public Task<SingleAssignmentReportDTO?> GetSingleAssignmentReportAsync(Guid courseId, Guid topicId, CancellationToken ct = default)
    //        => BuildSingleAssignmentReport(courseId, topicId, ct);

    //    public Task<SingleQuizReportDTO?> GetSingleQuizReportAsync(Guid courseId, Guid topicId, CancellationToken ct = default)
    //        => BuildSingleQuizReport(courseId, topicId, ct);

    //    private async Task<SingleAssignmentReportDTO?> BuildSingleAssignmentReport(Guid courseId, Guid topicId, CancellationToken ct)
    //    {
    //        var topic = await _unitOfWork.Topics.GetByIdAsync(topicId, ct);
    //        if (topic is null)
    //            return null;

    //        var topicAssignment = await _unitOfWork.TopicAssignments.FirstOrDefaultAsync(a => a.TopicId == topic.Id, ct);
    //        if (topicAssignment is null)
    //            return null;

    //        var assignmentResponses = await _unitOfWork.AssignmentResponses.FindAsync(r => r.TopicId == topic.Id, ct);

    //        var topicEndTime = topicAssignment.Close ?? DateTime.MaxValue;

    //        // students that joined the course before/at close
    //        var studentsTookPart = await _unitOfWork.EnrollmentDetails.FindByCourseJoinDateLTEAsync(courseId, topicEndTime, ct);
    //        var studentCount = studentsTookPart.Count;

    //        var dto = new SingleAssignmentReportDTO(topic.Title);

    //        if (studentCount == 0)
    //            return dto;

    //        // Map: studentId -> (markBase10, responseId)
    //        var studentMark10WithRespId = assignmentResponses
    //            .Where(r => r.Mark.HasValue)
    //            .ToDictionary(
    //                r => r.StudentId,
    //                r => (mark10: (double)(r.Mark!.Value / 10m), responseId: (Guid?)r.Id)
    //            );

    //        // Map: extension distribution
    //        var fileTypeCount = assignmentResponses
    //            .Where(r => r.Files != null)
    //            .SelectMany(r => r.Files!)
    //            .Where(f => !string.IsNullOrWhiteSpace(f.Name) && f.Name!.Contains('.'))
    //            .GroupBy(f => f.Name!.Substring(f.Name!.LastIndexOf('.') + 1))
    //            .ToDictionary(g => g.Key, g => (long)g.Count());

    //        // Info per student
    //        var studentInfoAndMarks = studentsTookPart
    //            .Select(stud =>
    //            {
    //                var sId = stud.Student.Id;
    //                var has = studentMark10WithRespId.TryGetValue(sId, out var pair);
    //                return new SingleAssignmentReportDTO.StudentInfoAndMark
    //                {
    //                    Student = MapUser(stud.Student),
    //                    Submitted = has,
    //                    Mark = has ? pair.mark10 : 0.0,
    //                    ResponseId = has ? pair.responseId : null
    //                };
    //            })
    //            .ToList();

    //        // Buckets
    //        dto.StudentMarks = studentInfoAndMarks;
    //        dto.StudentWithMarkOver8 = studentInfoAndMarks.Where(x => x.Submitted && x.Mark is >= 8.0).ToList();
    //        dto.StudentWithMarkOver5 = studentInfoAndMarks.Where(x => x.Submitted && x.Mark is >= 5.0 and < 8.0).ToList();
    //        dto.StudentWithMarkOver2 = studentInfoAndMarks.Where(x => x.Submitted && x.Mark is >= 2.0 and < 5.0).ToList();
    //        dto.StudentWithMarkOver0 = studentInfoAndMarks.Where(x => x.Submitted && x.Mark is < 2.0).ToList();
    //        dto.StudentWithNoResponse = studentInfoAndMarks.Where(x => !x.Submitted).ToList();

    //        // Distribution & aggregates
    //        dto.MarkDistributionCount = CalculateMarkDistribution(
    //            studentMark10WithRespId.ToDictionary(k => k.Key, v => v.Value.mark10),
    //            studentCount);

    //        dto.SubmissionCount = assignmentResponses.Count;
    //        dto.GradedSubmissionCount = assignmentResponses.Count(r => r.Mark.HasValue);
    //        dto.FileCount = assignmentResponses.Sum(r => r.Files?.Count ?? 0);
    //        dto.AvgMark = assignmentResponses.Where(r => r.Mark.HasValue).Select(r => (double)r.Mark!.Value).DefaultIfEmpty(0).Average();
    //        dto.MaxMark = assignmentResponses.Where(r => r.Mark.HasValue).Select(r => (double)r.Mark!.Value).DefaultIfEmpty(0).Max();
    //        dto.CompletionRate = studentCount == 0 ? 0 : (double)assignmentResponses.Count / studentCount;
    //        dto.Students = studentsTookPart.Select(x => MapUser(x.Student)).ToList();
    //        dto.FileTypeCount = fileTypeCount;

    //        return dto;
    //    }

    //    private async Task<SingleQuizReportDTO?> BuildSingleQuizReport(Guid courseId, Guid topicId, CancellationToken ct)
    //    {
    //        var topic = await _unitOfWork.Topics.GetByIdAsync(topicId, ct);
    //        if (topic is null)
    //            return null;

    //        var topicQuiz = await _unitOfWork.TopicQuizzes.FirstOrDefaultAsync(q => q.TopicId == topic.Id, ct);
    //        if (topicQuiz is null)
    //            return null;

    //        var quizQuestions = await _unitOfWork.TopicQuizQuestions.FindAsync(q => q.TopicQuizId == topicQuiz.TopicId, ct);
    //        var quizResponses = await _unitOfWork.QuizResponses.FindAsync(r => r.TopicId == topicQuiz.TopicId, ct);

    //        var topicEndTime = topicQuiz.Close ?? DateTime.MaxValue;
    //        var studentsTookPart = await _unitOfWork.EnrollmentDetails.FindByCourseJoinDateLTEAsync(courseId, topicEndTime, ct);
    //        var studentCount = studentsTookPart.Count;

    //        // studentId -> list<double> (normalized mark per attempt)
    //        var responseMarksByStudent = quizResponses
    //            .GroupBy(r => r.StudentId)
    //            .ToDictionary(
    //                g => g.Key,
    //                g => g.Select(r =>
    //                {
    //                    // Average normalized per answers
    //                    var marks = (r.Answers ?? new List<QuizResponseAnswer>())
    //                        .Select(a =>
    //                        {
    //                            // a.Question là JSON; lấy DefaultMark từ đó để quy về base10: (mark/defaultMark)*10
    //                            double defaultMark = 1.0;
    //                            try
    //                            {
    //                                // question JSON có thể giống TopicQuizQuestion
    //                                var q = JsonSerializer.Deserialize<TopicQuizQuestion>(a.Question ?? "{}", _jsonOptions);
    //                                if (q?.DefaultMark != null && q.DefaultMark.Value > 0)
    //                                    defaultMark = (double)q.DefaultMark.Value;
    //                            }
    //                            catch { /* ignore and use default */ }

    //                            var m = a.Mark ?? 0.0;
    //                            return defaultMark > 0 ? (m / defaultMark) * 10.0 : 0.0;
    //                        })
    //                        .DefaultIfEmpty(0.0)
    //                        .Average();

    //                    return marks;
    //                }).ToList()
    //            );

    //        // Áp dụng grading method (Highest/Average/First/Last)
    //        double SelectByMethod(List<double> arr, string? method)
    //        {
    //            if (arr == null || arr.Count == 0) return 0.0;
    //            return method switch
    //            {
    //                "Highest Grade" => arr.Max(),
    //                "Average Grade" => arr.Average(),
    //                "First Grade" => arr.First(),
    //                "Last Grade" => arr.Last(),
    //                _ => arr.Max()
    //            };
    //        }

    //        var finalMarkByStudent = responseMarksByStudent
    //            .ToDictionary(k => k.Key, v => SelectByMethod(v.Value, topicQuiz.GradingMethod));

    //        var dto = new SingleQuizReportDTO
    //        {
    //            Name = topic.Title,
    //            QuestionCount = quizQuestions.Count,
    //            MaxDefaultMark = quizQuestions.Sum(q => (double)(q.DefaultMark ?? 0)),
    //            AttemptCount = quizResponses.Count,
    //            AvgTimeSpend = CalcAvgTimeSpendSeconds(quizResponses),
    //            AvgStudentMarkBase10 = finalMarkByStudent.Values.DefaultIfEmpty(0.0).Average(),
    //            MaxStudentMarkBase10 = finalMarkByStudent.Values.DefaultIfEmpty(0.0).Max(),
    //            MinStudentMarkBase10 = finalMarkByStudent.Values.DefaultIfEmpty(0.0).Min(),
    //            CompletionRate = studentCount == 0 ? 0.0
    //                           : (double)finalMarkByStudent.Count / studentCount,
    //            TrueFalseQuestionCount = quizQuestions.Count(q => string.Equals(q.Type, "True/False", StringComparison.OrdinalIgnoreCase)),
    //            MultipleChoiceQuestionCount = quizQuestions.Count(q => string.Equals(q.Type, "Choices Answer", StringComparison.OrdinalIgnoreCase)),
    //            ShortAnswerQuestionCount = quizQuestions.Count(q => string.Equals(q.Type, "Short Answer", StringComparison.OrdinalIgnoreCase)),
    //        };

    //        // Build student lists
    //        var byId = studentsTookPart.ToDictionary(x => x.Student.Id, x => x);

    //        var studentsWithMarks = finalMarkByStudent.Select(kv =>
    //            new SingleQuizReportDTO.StudentInfoAndMark
    //            {
    //                Student = MapUser(byId[kv.Key].Student),
    //                Submitted = true,
    //                Mark = kv.Value,
    //                ResponseId = null // có thể có nhiều attempt → để null cho đúng với Java note
    //            }).ToList();

    //        var studentsNoResp = byId.Keys
    //            .Where(id => !finalMarkByStudent.ContainsKey(id))
    //            .Select(id => new SingleQuizReportDTO.StudentInfoAndMark
    //            {
    //                Student = MapUser(byId[id].Student),
    //                Submitted = false,
    //                Mark = 0.0,
    //                ResponseId = null
    //            }).ToList();

    //        dto.StudentWithMark = studentsWithMarks.Concat(studentsNoResp).ToList();
    //        dto.StudentWithMarkOver8 = dto.StudentWithMark.Where(x => x.Submitted && x.Mark is >= 8.0).ToList();
    //        dto.StudentWithMarkOver5 = dto.StudentWithMark.Where(x => x.Submitted && x.Mark is >= 5.0 and < 8.0).ToList();
    //        dto.StudentWithMarkOver2 = dto.StudentWithMark.Where(x => x.Submitted && x.Mark is >= 2.0 and < 5.0).ToList();
    //        dto.StudentWithMarkOver0 = dto.StudentWithMark.Where(x => x.Submitted && x.Mark is < 2.0).ToList();
    //        dto.StudentWithNoResponse = dto.StudentWithMark.Where(x => !x.Submitted).ToList();

    //        dto.MarkDistributionCount = CalculateMarkDistribution(finalMarkByStudent, studentCount);
    //        dto.Students = studentsTookPart.Select(x => MapUser(x.Student)).ToList();

    //        return dto;
    //    }

    //    private static double CalcAvgTimeSpendSeconds(IEnumerable<QuizResponse> quizResponses)
    //    {
    //        // assume StartedAt/CompletedAt là DateTime? hoặc string ISO. Điều chỉnh parser nếu bạn lưu string.
    //        var spans = quizResponses
    //            .Select(r =>
    //            {
    //                if (DateTime.TryParse(r.CompletedAt, out var end) &&
    //                    DateTime.TryParse(r.StartedAt, out var start))
    //                    return (end - start).TotalSeconds;

    //                return 0.0;
    //            })
    //            .ToList();

    //        return spans.Count == 0 ? 0.0 : spans.Average();
    //    }

    //    private static Dictionary<int, int> CalculateMarkDistribution(Dictionary<Guid, double> studentMark10, int studentCount)
    //    {
    //        var marks = studentMark10.Values.ToList();

    //        var c8 = marks.Count(m => m >= 8.0);
    //        var c5 = marks.Count(m => m >= 5.0 && m < 8.0);
    //        var c2 = marks.Count(m => m >= 2.0 && m < 5.0);
    //        var c0 = marks.Count(m => m >= 0.0 && m < 2.0);
    //        var cMiss = Math.Max(0, studentCount - (c8 + c5 + c2 + c0));

    //        return new Dictionary<int, int>
    //        {
    //            { 8, c8 }, { 5, c5 }, { 2, c2 }, { 0, c0 }, { -1, cMiss }
    //        };
    //    }

    //    private static GetUserResponse MapUser(LetsLearn.Core.Entities.User u) =>
    //        new()
    //        {
    //            Id = u.Id,
    //            Email = u.Email,
    //            Username = u.Username
    //        };
    }
}
