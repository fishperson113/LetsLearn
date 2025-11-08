using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services.QuizResponseService;
using LetsLearn.UseCases.Services.Users;
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
                                    }).ToList() ?? new List<TopicQuizQuestionChoice>()
                                }).ToList() ?? new List<TopicQuizQuestion>()
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

        public async Task<SingleQuizReportDTO> GetSingleQuizReportAsync(String courseId, Guid topicId, CancellationToken ct = default)
        {
            // Fetch Topic
            var topic = await _unitOfWork.Topics.GetByIdAsync(topicId, ct)
                ?? throw new KeyNotFoundException("Topic not found");

            var reportDTO = new SingleQuizReportDTO();

            // Fetch TopicQuiz and its questions
            var topicQuiz = await _unitOfWork.TopicQuizzes.GetWithQuestionsAsync(topicId)
                ?? throw new KeyNotFoundException("No topic quiz found");

            // Use topicQuiz.Questions to get the list of quiz questions
            var quizQuestions = topicQuiz.Questions;

            // Fetch quiz responses of students
            var quizResponses = await _unitOfWork.QuizResponses.FindAllByTopicIdAsync(topicId, ct);

            // Get students who participated in the quiz
            var topicEndTime = topicQuiz.Close ?? DateTime.MaxValue;
            var studentsThatTookPartIn = await _unitOfWork.Enrollments.GetByCourseIdAndJoinDateLessThanEqualAsync(courseId, topicEndTime, ct);

            int studentCount = studentsThatTookPartIn.Count;

            // Calculate student marks based on quiz responses
            var marksWithStudentId = quizResponses.ToDictionary(
                response => response.StudentId,
                response => (double)response.Answers.Average(answer =>
                {
                    var question = JsonSerializer.Deserialize<Question>(answer.Question);
                    _logger.LogInformation("Raw JSON for CorrectAnswer: {JsonData}", answer.Answer);
                    return (answer.Mark / question!.DefaultMark ?? 1) * 10;
                })
            );

            // Apply the grading method to calculate final marks for each student
            var finalMarksWithStudentId = marksWithStudentId.ToDictionary(
                entry => entry.Key,
                entry => CalculateMark(new List<double> { entry.Value }, topicQuiz.GradingMethod!) // Apply grading method
            );

            // Calculate average, max, and min marks
            double avgMark = marksWithStudentId.Values.Average();
            double maxMark = marksWithStudentId.Values.Max();
            double minMark = marksWithStudentId.Values.Min();

            // Categorize students by marks
            var studentInfoAndMarks = await GetStudentInfoWithMarkAndResponseIdForQuiz(studentsThatTookPartIn, finalMarksWithStudentId, ct);

            reportDTO.StudentWithMark = studentInfoAndMarks;
            reportDTO.StudentWithMarkOver8 = studentInfoAndMarks.Where(info => info.Mark >= 8).ToList();
            reportDTO.StudentWithMarkOver5 = studentInfoAndMarks.Where(info => info.Mark >= 5 && info.Mark < 8).ToList();
            reportDTO.StudentWithMarkOver2 = studentInfoAndMarks.Where(info => info.Mark >= 2 && info.Mark < 5).ToList();
            reportDTO.StudentWithMarkOver0 = studentInfoAndMarks.Where(info => info.Mark < 2).ToList();
            reportDTO.StudentWithNoResponse = studentInfoAndMarks.Where(info => !info.Submitted).ToList();

            var quizResponseDTOs = await MapQuizResponsesToDTO(quizResponses, ct);

            // Update additional metrics
            reportDTO.MarkDistributionCount = CalculateMarkDistribution(marksWithStudentId, studentCount);
            reportDTO.QuestionCount = quizQuestions.Count;
            reportDTO.MaxDefaultMark = quizQuestions.Sum(q => (double)q.DefaultMark!);
            reportDTO.AvgStudentMarkBase10 = avgMark;
            reportDTO.MaxStudentMarkBase10 = maxMark;
            reportDTO.MinStudentMarkBase10 = minMark;
            reportDTO.AttemptCount = quizResponses.Count;
            reportDTO.AvgTimeSpend = CalculateAvgTimeSpend(quizResponseDTOs);
            reportDTO.CompletionRate = (double)marksWithStudentId.Count / studentCount;
            reportDTO.TrueFalseQuestionCount = quizQuestions.Count(q => q.Type == "True/False");
            reportDTO.MultipleChoiceQuestionCount = quizQuestions.Count(q => q.Type == "Choices Answer");
            reportDTO.ShortAnswerQuestionCount = quizQuestions.Count(q => q.Type == "Short Answer");

            return reportDTO;
        }

        private double CalculateMark(List<double> marks, string method)
        {
            return method.ToLower() switch
            {
                "highest grade" => marks.Max(),
                "average grade" => marks.Average(),
                "first grade" => marks.FirstOrDefault(),
                "last grade" => marks.LastOrDefault(),
                _ => throw new ArgumentException($"Invalid method: {method}")
            };
        }

        private double CalculateAvgTimeSpend(List<QuizResponseDTO> quizResponses)
        {
            var validResponses = quizResponses
                .Where(res => res.Data.StartedAt.HasValue && res.Data.CompletedAt.HasValue);

            if (!validResponses.Any()) return 0.0;

            return validResponses
                .Where(res => res.Data.StartedAt.HasValue && res.Data.CompletedAt.HasValue) 
                .Select(res =>
                {
                    var startedAt = res.Data.StartedAt.Value;
                    var completedAt = res.Data.CompletedAt.Value;

                    return (completedAt - startedAt).TotalSeconds;
                })
                .Average();
        }

        private int CountQuestionType(List<TopicQuizQuestion> questions, string questionType)
        {
            return questions.Count(q => q.Type!.Equals(questionType, StringComparison.OrdinalIgnoreCase));
        }

        public Dictionary<int, int> CalculateMarkDistribution(Dictionary<Guid, double> studentsWithMark, int studentCount)
        {
            var count8OrMore = studentsWithMark.Values.Count(mark => mark >= 8);
            var count5To7 = studentsWithMark.Values.Count(mark => mark >= 5 && mark < 8);
            var count2To4 = studentsWithMark.Values.Count(mark => mark >= 2 && mark < 5);
            var count0To1 = studentsWithMark.Values.Count(mark => mark >= 0 && mark < 2);

            var distribution = new Dictionary<int, int>
            {
                { 8, count8OrMore },
                { 5, count5To7 },
                { 2, count2To4 },
                { 0, count0To1 },
                { -1, studentCount - count8OrMore - count5To7 - count2To4 - count0To1 }
            };

            return distribution;
        }

        public async Task<List<SingleQuizReportDTO.StudentInfoAndMark>> GetStudentInfoWithMarkAndResponseIdForQuiz(
            List<Enrollment> studentsThatTookPartIn,
            Dictionary<Guid, double> studentIdWithMark,
            CancellationToken ct = default)
        {
            var enrollmentByStudentId = studentsThatTookPartIn.ToDictionary(detail => detail.StudentId);

            // Fetch students with marks
            var studentsWithMarks = await Task.WhenAll(studentIdWithMark.Select(async entry =>
            {
                var studentId = entry.Key;
                var mark = entry.Value;
                var enrollment = enrollmentByStudentId[studentId];

                var user = await _unitOfWork.Users.GetByIdAsync(studentId, ct);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found.");
                }

                return new SingleQuizReportDTO.StudentInfoAndMark
                {
                    Student = MapToDTO(user),
                    Mark = mark,
                    ResponseId = null
                };
            })).ConfigureAwait(false);

            // Fetch students with no response
            var studentsNoResponse = await Task.WhenAll(enrollmentByStudentId
                .Where(entry => !studentIdWithMark.ContainsKey(entry.Key)) // Filter students with no marks
                .Select(async entry =>
                {
                    var studentId = entry.Value.StudentId;

                    var user = await _unitOfWork.Users.GetByIdAsync(studentId, ct);

                    if (user == null)
                    {
                        throw new KeyNotFoundException("User not found.");
                    }

                    return new SingleQuizReportDTO.StudentInfoAndMark
                    {
                        Student = MapToDTO(user),
                        Submitted = false,
                        Mark = 0.0,
                        ResponseId = null
                    };
                }));

            //// Create the report
            //var reportDTO = new SingleQuizReportDTO
            //{
            //    StudentWithMark = studentsWithMarks.ToList(),
            //    StudentWithNoResponse = studentsNoResponse.ToList()
            //};

            //// Combine both lists into one result
            //return reportDTO.StudentWithMark.Concat(reportDTO.StudentWithNoResponse).ToList();

            // Combine both lists
            var result = new List<SingleQuizReportDTO.StudentInfoAndMark>();
            result.AddRange(studentsWithMarks);
            result.AddRange(studentsNoResponse);

            return result;
        }

        public async Task<List<SingleAssignmentReportDTO.StudentInfoAndMark>> GetStudentInfoWithMarkAndResponseIdForAssignment(
            List<Enrollment> studentsThatTookPartIn,
            Dictionary<Guid, double> studentIdWithMark,
            CancellationToken ct = default)
        {
            var enrollmentByStudentId = studentsThatTookPartIn.ToDictionary(detail => detail.StudentId);

            // Fetch students with marks
            var studentsWithMarks = await Task.WhenAll(studentIdWithMark.Select(async entry =>
            {
                var studentId = entry.Key;
                var mark = entry.Value;
                var enrollment = enrollmentByStudentId[studentId];

                // Get user info asynchronously
                var user = await _unitOfWork.Users.GetByIdAsync(studentId, ct);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found.");
                }

                return new SingleAssignmentReportDTO.StudentInfoAndMark
                {
                    Student = MapToDTO(user),
                    Mark = mark,
                    ResponseId = null
                };
            }));

            // Fetch students with no response
            var studentsNoResponse = await Task.WhenAll(enrollmentByStudentId
                .Where(entry => !studentIdWithMark.ContainsKey(entry.Key)) // Filter students with no marks
                .Select(async entry =>
                {
                    var studentId = entry.Value.StudentId;
                    var user = await _unitOfWork.Users.GetByIdAsync(studentId, ct);

                    if (user == null)
                    {
                        throw new KeyNotFoundException("User not found.");
                    }

                    return new SingleAssignmentReportDTO.StudentInfoAndMark
                    {
                        Student = MapToDTO(user),
                        Submitted = false,
                        Mark = 0.0,
                        ResponseId = null
                    };
                }));

            // Combine both lists
            var result = new List<SingleAssignmentReportDTO.StudentInfoAndMark>();
            result.AddRange(studentsWithMarks);
            result.AddRange(studentsNoResponse);

            return result;
        }

        public static GetUserResponse MapToDTO(LetsLearn.Core.Entities.User user)
        {
            return new GetUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
                Enrollments = user.Enrollments?.Select(e => new EnrollmentDTO
                {
                    StudentId = e.StudentId,
                    CourseId = e.CourseId,
                    JoinDate = e.JoinDate
                }).ToList()
            };
        }

        public async Task<List<QuizResponseDTO>> MapQuizResponsesToDTO(List<QuizResponse>? quizResponses, CancellationToken ct = default)
        {
            // Nếu quizResponses là null hoặc không có dữ liệu, trả về danh sách rỗng
            if (quizResponses == null || !quizResponses.Any())
            {
                return new List<QuizResponseDTO>();
            }

            // Chuyển đổi từng phần tử trong danh sách quizResponses sang DTO
            var quizResponseDTOs = await Task.WhenAll(quizResponses.Select(async quizResponse =>
            {
                return MapQuizResponseToDTO(quizResponse); // Sử dụng hàm đã viết để chuyển đổi
            }));

            return quizResponseDTOs.ToList();
        }

        public QuizResponseDTO MapQuizResponseToDTO(QuizResponse quizResponse)
        {
            if (quizResponse == null)
            {
                throw new ArgumentNullException(nameof(quizResponse));
            }

            var quizResponseDTO = new QuizResponseDTO
            {
                Id = quizResponse.Id,
                StudentId = quizResponse.StudentId,
                TopicId = quizResponse.TopicId,
                Data = new QuizResponseData
                {
                    Status = quizResponse.Status,
                    StartedAt = quizResponse.StartedAt,
                    CompletedAt = quizResponse.CompletedAt,
                    Answers = quizResponse.Answers
                        .Select(answer => new QuizResponseAnswerDTO
                        {
                            // Chuyển đổi Answer thành DTO
                            Answer = answer.Answer,
                            Mark = answer.Mark,
                            TopicQuizQuestion = JsonSerializer.Deserialize<Question>(answer.Question) // Giả sử 'Question' là JSON
                        }).ToList()
                }
            };

            return quizResponseDTO;
        }
    }
}
