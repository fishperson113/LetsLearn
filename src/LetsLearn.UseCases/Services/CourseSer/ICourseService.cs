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
        Task<CreateCourseResponse> CreateAsync(CreateCourseRequest dto, Guid userId, CancellationToken ct = default);
        Task<UpdateCourseResponse> UpdateAsync(UpdateCourseRequest dto, CancellationToken ct = default);
        Task<List<GetCourseResponse>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<List<GetCourseResponse>> GetAllPublicAsync(CancellationToken ct = default);
        Task<GetCourseResponse> GetByIdAsync(String id, CancellationToken ct = default);
        Task AddUserToCourseAsync(String courseId, Guid userId, CancellationToken ct = default);
        Task<List<TopicDTO>> GetAllWorksOfCourseAndUserAsync(String courseId, Guid userId, string type, DateTime? start, DateTime? end, CancellationToken ct = default);



            //Task<List<TopicResponse>> GetAllWorksOfCourseAndUserAsync(
            //    Guid courseId, Guid userId,
            //    string? type, DateTime? start, DateTime? end,
            //    CancellationToken ct = default);
            //Task AddUserToCourseAsync(Guid courseId, Guid userId, CancellationToken ct = default);
            //Task<AllQuizzesReportDTO> GetQuizzesReportAsync(Guid courseId, DateTime? start, DateTime? end, CancellationToken ct = default);
            //Task<AllAssignmentsReportDTO> GetAssignmentsReportAsync(Guid courseId, DateTime? start, DateTime? end, CancellationToken ct = default);


        }
}
