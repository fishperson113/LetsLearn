using LetsLearn.UseCases.DTOs;

namespace LetsLearn.UseCases.Services.Users
{
    public interface IUserService
    {
        Task<UserDTO> GetByIdAsync(Guid id);
        Task<UserDTO> UpdateAsync(Guid id, UpdateUserDTO dto);
        Task<List<UserDTO>> GetAllAsync(Guid requesterId);

        //Task<List<TopicDTO>> GetUserWorksAsync(Guid userId, string? type, Guid? courseId,
        //                                       DateTime? start, DateTime? end);

        //Task<List<AssignmentResponseDTO>> GetAssignmentsAsync(Guid userId);
        //Task<List<QuizResponseDTO>> GetQuizzesAsync(Guid userId);
        //Task<StudentReportDTO> GetReportAsync(Guid userId, Guid? courseId, DateTime? start, DateTime? end);
    }
}
