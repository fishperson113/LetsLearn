using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.UnitOfWork;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.CourseSer
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITopicService _topicService;

        private static readonly DateTime MIN = DateTime.SpecifyKind(DateTime.MinValue.AddYears(1), DateTimeKind.Utc);
        private static readonly DateTime MAX = DateTime.SpecifyKind(DateTime.MaxValue.AddYears(-1), DateTimeKind.Utc);

        public CourseService(IUnitOfWork uow, ITopicService topicService)
        {
            _uow = uow;
            _topicService = topicService;
        }

        // =============== CREATE / UPDATE ===============
        // Test Case Estimation:
        // Decision points (D):
        // - if Title is null/whitespace: +1
        // - if titleExists: +1
        // - if idExists: +1
        // D = 3 => Minimum Test Cases = D + 1 = 4
        public async Task<CreateCourseResponse> CreateAsync(CreateCourseRequest dto, Guid userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Title is required.");

            var titleExists = await _uow.Course.ExistByTitle(dto.Title!);
            if (titleExists) 
                throw new InvalidOperationException("A course with this title already exists. Please choose a different name");

            var idExists = await _uow.Course.ExistsAsync(c => c.Id == dto.Id, ct);
            if (idExists)
                throw new InvalidOperationException($"Course ID '{dto.Id}' already exists.");

            var course = new Course
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Category = dto.Category,
                Level = dto.Level,
                Price = dto.Price,
                IsPublished = dto.IsPublished ?? false,
                CreatorId = userId,
                TotalJoined = 1
            };

            await _uow.Course.AddAsync(course);

            DateTime utcNow = DateTime.UtcNow;

            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7);

            var enrollment = new Enrollment
            {
                StudentId = userId,
                CourseId = course.Id,
                JoinDate = DateTime.SpecifyKind(gmtPlus7Time, DateTimeKind.Utc)
            };

            await _uow.Enrollments.AddAsync(enrollment);

            try
            {
                await _uow.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to create course.", ex);
            }

            return new CreateCourseResponse
            {
                Id = course.Id,
                CreatorId = course.CreatorId,
                Title = course.Title,
                Description = course.Description,
                TotalJoined = course.TotalJoined,
                ImageUrl = course.ImageUrl,
                Price = course.Price,
                Category = course.Category,
                Level = course.Level,
                IsPublished = course.IsPublished
            };
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Null-coalesce throw when course not found: +1
        // - if Title provided: +1
        // - if titleExists: +1
        // D = 3 => Minimum Test Cases = D + 1 = 4
        public async Task<UpdateCourseResponse> UpdateAsync(UpdateCourseRequest dto, CancellationToken ct = default)
        {
            var course = await _uow.Course.GetByIdAsync(dto.Id, ct)
                         ?? throw new KeyNotFoundException("Course not found.");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                course.Title = dto.Title;

            course.Description = dto.Description;
            course.ImageUrl = dto.ImageUrl;
            course.Category = dto.Category;
            course.Level = dto.Level;
            course.IsPublished = dto.IsPublished ?? course.IsPublished;

            try
            {
                await _uow.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to update course.", ex);
            }

            return new UpdateCourseResponse
            {
                Id = course.Id,
                CreatorId = course.CreatorId,
                Title = course.Title,
                Description = course.Description,
                TotalJoined = course.TotalJoined,
                ImageUrl = course.ImageUrl,
                Price = course.Price,
                Category = course.Category,
                Level = course.Level,
                IsPublished = course.IsPublished,
            };
        }

        // ================= READ =================
        // Test Case Estimation:
        // Decision points (D):
        // - Pure retrieval/filtering in repository: +0
        // D = 0 => Minimum Test Cases = D + 1 = 1
        public async Task<List<GetCourseResponse>> GetAllPublicAsync(CancellationToken ct = default)
        {
            var courses = await _uow.Course.GetAllCoursesByIsPublishedTrue();
            return courses.Where(c => c != null).Select(c => MapToResponse(c!)).ToList();
        }

        // Test Case Estimation:
        // Decision points (D):
        // - if userExists is false: +1
        // D = 1 => Minimum Test Cases = D + 1 = 2
        public async Task<List<GetCourseResponse>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var userExists = await _uow.Users.ExistsAsync(u => u.Id == userId, ct);
            if (!userExists) throw new KeyNotFoundException("User not found.");

            var enrollments = await _uow.Enrollments.GetByStudentId(userId, ct);

            var courseIds = enrollments.Select(e => e.CourseId).Distinct();

            var courses = await _uow.Course.GetByIdsAsync(courseIds);

            return courses.Where(c => c != null).Select(c => MapToResponse(c!)).ToList();
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Null-coalesce throw when course not found: +1
        // D = 1 => Minimum Test Cases = D + 1 = 2
        public async Task<GetCourseResponse> GetByIdAsync(String id, CancellationToken ct = default)
        {
            var course = await _uow.Course.GetByIdAsync(id, ct)
                         ?? throw new KeyNotFoundException("Course not found.");
            return MapToResponse(course);
        }

        public async Task AddUserToCourseAsync(String courseId, Guid userId, CancellationToken ct = default)
        {
            var course = await _uow.Course.GetByIdAsync(courseId, ct)
                             ?? throw new KeyNotFoundException("Course not found.");

            var user = await _uow.Users.GetByIdAsync(userId, ct)
                            ?? throw new KeyNotFoundException("User not found.");

            var enrollmentExists = await _uow.Enrollments.ExistsAsync(e => e.CourseId == courseId && e.StudentId == userId, ct);
            if (enrollmentExists)
                throw new InvalidOperationException("User is already enrolled in this course.");

            var enrollment = new Enrollment
            {
                StudentId = userId,
                CourseId = courseId,
                JoinDate = DateTime.UtcNow
            };

            await _uow.Enrollments.AddAsync(enrollment);

            course.TotalJoined += 1;

            try
            {
                await _uow.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to add user to course.", ex);
            }
        }

        public async Task<List<TopicDTO>> GetAllWorksOfCourseAndUserAsync(String courseId, Guid userId, string type, DateTime? start, DateTime? end, CancellationToken ct = default)
        {
            // Kiểm tra nếu chỉ có start hoặc end được cung cấp
            if (start.HasValue ^ end.HasValue) // XOR
            {
                throw new ArgumentException("Provide start and end time!");
            }

            // Kiểm tra nếu start sau end
            if (start.HasValue && end.HasValue && start > end)
            {
                throw new ArgumentException("Start time must be after end time");
            }

            // Lấy thông tin khóa học
            var course = await _uow.Course.GetByIdAsync(courseId, ct);
            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var result = new List<TopicDTO>();

            // Duyệt qua tất cả các sections trong khóa học
            foreach (var courseSection in course.Sections)
            {
                // Duyệt qua các topics trong từng section
                foreach (var topicSection in courseSection.Topics)
                {
                    if (string.IsNullOrEmpty(type) || type.Equals(topicSection.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        var topicData = await GetTopicDataByTypeAsync(topicSection.Id, userId, start, end, ct);
                        if (topicData != null)
                        {
                            var topicDTO = ToDTO(topicSection);
                            topicDTO.Data = topicData;
                            //topicDTO.Data = topicData.Item;
                            //topicDTO.Response = topicData.Response;
                            result.Add(topicDTO);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<object?> GetTopicDataByTypeAsync(Guid topicId, Guid userId, DateTime? start, DateTime? end, CancellationToken ct = default) //TopicDataDTO
        {
            // Lấy thông tin topic theo loại
            var topic = await _uow.Topics.GetByIdAsync(topicId, ct);
            if (topic == null)
            {
                throw new KeyNotFoundException("Topic not found");
            }

            // TopicDataDTO? topicData = null;

            object? topicData = null;

            switch (topic.Type.ToLower())
            {
                case "quiz":
                    var quiz = await _uow.TopicQuizzes.GetWithQuestionsAsync(topicId);
                    if (quiz != null && (end == null || quiz.Close < end))
                    {
                        //var responses = await _uow.QuizResponses.GetByTopicIdAndStudentIdAsync(quiz.TopicId, userId, ct);
                        //topicData = new TopicDataDTO
                        //{
                        //    Item = quiz,
                        //    Response = responses
                        //};
                        topicData = new { quiz };
                    }
                    break;

                case "assignment":
                    var assignment = (await _uow.TopicAssignments.FindAsync(a => a.TopicId == topicId, ct)).FirstOrDefault();
                    if (assignment != null && (end == null || assignment.Close < end))
                    {
                        //var response = await _uow.AssignmentResponse.GetByTopicIdAndStudentIdAsync(assignment.TopicId, userId, ct);
                        //topicData = new TopicDataDTO
                        //{
                        //    Item = assignment,
                        //    Response = response
                        //};
                        topicData = new { assignment };
                    }
                    break;

                case "meeting":
                case "file":
                case "link":
                case "page":
                    // Không xử lý các loại này
                    break;

                default:
                    throw new NotSupportedException($"Topic type {topic.Type} is not supported.");
            }

            return topicData;
        }

        // =============== Helpers (mapping & utils) ===============
        // Test Case Estimation:
        // Decision points (D):
        // - Pure mapping, no branching: +0
        // D = 0 => Minimum Test Cases = D + 1 = 1 (basic mapping)
        private static GetCourseResponse MapToResponse(Course c)
        {
            return new GetCourseResponse
            {
                Id = c.Id,
                CreatorId = c.CreatorId,
                Title = c.Title,
                Description = c.Description,
                TotalJoined = c.TotalJoined,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                Category = c.Category,
                Level = c.Level,
                IsPublished = c.IsPublished,
                Sections = c.Sections?.Select(s => new SectionResponse
                {
                    Id = s.Id,
                    CourseId = c.Id,
                    Position = s.Position,
                    Title = s.Title,
                    Description = s.Description,
                    Topics = s.Topics?.Select(t => new TopicResponse
                    {
                        Id = t.Id,
                        Title = t.Title,
                        SectionId = t.SectionId,
                        Type = t.Type
                    }).ToList() ?? new List<TopicResponse>()
                }).ToList()
            };
        }

        public static TopicDTO ToDTO(Topic topic)
        {
            return new TopicDTO
            {
                Id = topic.Id,
                Title = topic.Title,
                Type = topic.Type,
                SectionId = topic.SectionId,
                // Add any other properties of Topic to map to TopicDTO
            };
        }
    }
}
