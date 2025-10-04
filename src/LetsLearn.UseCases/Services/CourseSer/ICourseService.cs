using LetsLearn.UseCases.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.CourseSer
{
    public interface ICourseService
    {
        Task<CourseResponse> CreateAsync(CourseRequest dto, CancellationToken ct = default);
        Task<CourseResponse> UpdateAsync(String id, CourseRequest dto, CancellationToken ct = default);
        Task<List<CourseResponse>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<List<CourseResponse>> GetAllPublicAsync(CancellationToken ct = default);
        Task<CourseResponse> GetByIdAsync(String id, CancellationToken ct = default);
        //Task<List<TopicResponse>> GetAllWorksOfCourseAndUserAsync(
        //    Guid courseId, Guid userId,
        //    string? type, DateTime? start, DateTime? end,
        //    CancellationToken ct = default);
        //Task AddUserToCourseAsync(Guid courseId, Guid userId, CancellationToken ct = default);
        //Task<AllQuizzesReportDTO> GetQuizzesReportAsync(Guid courseId, DateTime? start, DateTime? end, CancellationToken ct = default);
        //Task<AllAssignmentsReportDTO> GetAssignmentsReportAsync(Guid courseId, DateTime? start, DateTime? end, CancellationToken ct = default);


    }
}
