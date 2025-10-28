using LetsLearn.UseCases.DTOs;

namespace LetsLearn.UseCases.Services.Users
{
    public interface IUserService
    {
        Task<GetUserResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<UpdateUserResponse> UpdateAsync(Guid id, UpdateUserDTO dto);
        Task<List<GetUserResponse>> GetAllAsync(Guid requesterId);

        //Task<List<TopicDTO>> GetUserWorksAsync(Guid userId, string? type, Guid? courseId,
        //                                       DateTime? start, DateTime? end);

        //Task<List<AssignmentResponseDTO>> GetAssignmentsAsync(Guid userId);
        //Task<List<QuizResponseDTO>> GetQuizzesAsync(Guid userId);
        //Task<StudentReportDTO> GetReportAsync(Guid userId, Guid? courseId, DateTime? start, DateTime? end);
    }
}
