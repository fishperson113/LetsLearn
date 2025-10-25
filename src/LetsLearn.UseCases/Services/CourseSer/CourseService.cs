using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LetsLearn.UseCases.Services.CourseSer
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _uow;
        private static readonly DateTime MIN = DateTime.SpecifyKind(DateTime.MinValue.AddYears(1), DateTimeKind.Utc);
        private static readonly DateTime MAX = DateTime.SpecifyKind(DateTime.MaxValue.AddYears(-1), DateTimeKind.Utc);

        public CourseService(IUnitOfWork uow)
        {
            _uow = uow;
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
                TotalJoined = 0
            };

            await _uow.Course.AddAsync(course);
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
                IsPublished = course.IsPublished,
                // Sections = null (chưa load)
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

            var titleExists = await _uow.Course.ExistByTitle(dto.Title!);
            if (titleExists)
                throw new InvalidOperationException("A course with this title already exists. Please choose a different name");

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
                // Sections = null (chưa load)
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

            var courses = await _uow.Course.GetByCreatorId(userId);
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
                // Sections = null (chưa load)
            };
        }

        //private static TopicDTO MapTopicToDto(Topic t)
        //{
        //    return new TopicDTO
        //    {
        //        Id = t.Id,
        //        Type = t.Type,
        //    };
        //}

        //private static List<SingleQuizReportDTO.StudentInfoAndMark> CalculateAverageStudentScoreForQuizzes(List<SingleQuizReportDTO> singleQuizReports)
        //{
        //    var scoreMap = new Dictionary<Guid, List<double>>();
        //    var latestInfo = new Dictionary<Guid, SingleQuizReportDTO.StudentInfoAndMark>();

        //    foreach (var rep in singleQuizReports)
        //    {
        //        if (rep.StudentWithMark == null) continue;
        //        foreach (var info in rep.StudentWithMark)
        //        {
        //            if (info.Student == null || !info.Submitted || info.Mark == null) continue;
        //            var studentId = info.Student.Id;
        //            if (!scoreMap.TryGetValue(studentId, out var list))
        //            {
        //                list = new List<double>();
        //                scoreMap[studentId] = list;
        //            }
        //            list.Add(info.Mark.Value);
        //            latestInfo[studentId] = info;
        //        }
        //    }

        //    return scoreMap.Select(kv =>
        //    {
        //        var studentId = kv.Key;
        //        var scores = kv.Value;
        //        var avg = scores.Count > 0 ? scores.Average() : 0.0;
        //        var src = latestInfo[studentId];
        //        return new SingleQuizReportDTO.StudentInfoAndMark
        //        {
        //            Student = src.Student,
        //            Submitted = src.Submitted,
        //            ResponseId = src.ResponseId,
        //            Mark = avg
        //        };
        //    }).ToList();
        //}

        //private static List<AllAssignmentsReportDTO.StudentInfoWithAverageMark> CalculateAverageStudentScoreForAssignments(List<SingleAssignmentReportDTO> singleAssignmentReports)
        //{
        //    var scoreMap = new Dictionary<Guid, List<double>>();
        //    var latestInfo = new Dictionary<Guid, SingleAssignmentReportDTO.StudentInfoAndMark>();

        //    foreach (var rep in singleAssignmentReports)
        //    {
        //        if (rep.StudentMarks == null) continue;
        //        foreach (var info in rep.StudentMarks)
        //        {
        //            if (info.Student == null || !info.Submitted || info.Mark == null) continue;
        //            var studentId = info.Student.Id;
        //            if (!scoreMap.TryGetValue(studentId, out var list))
        //            {
        //                list = new List<double>();
        //                scoreMap[studentId] = list;
        //            }
        //            list.Add(info.Mark.Value);
        //            latestInfo[studentId] = info;
        //        }
        //    }

        //    return scoreMap.Select(kv =>
        //    {
        //        var studentId = kv.Key;
        //        var scores = kv.Value;
        //        var avg = scores.Count > 0 ? scores.Average() : 0.0;
        //        var src = latestInfo[studentId];
        //        return new AllAssignmentsReportDTO.StudentInfoWithAverageMark(src.Student, avg, src.Submitted);
        //    }).ToList();
        //}

        //private static Dictionary<object, object> MergeMarkDistributionCount(List<Dictionary<object, object>> list)
        //{
        //    var keys = new object[] { -1, 0, 2, 5, 8 };
        //    var acc = keys.ToDictionary(k => (object)k, k => (object)0);

        //    foreach (var dict in list.Where(x => x != null))
        //    {
        //        foreach (var k in keys)
        //        {
        //            var v = dict.TryGetValue(k, out var val) ? Convert.ToInt32(val) : 0;
        //            acc[k] = Convert.ToInt32(acc[k]) + v;
        //        }
        //    }
        //    return acc;
        //}
    }
}
