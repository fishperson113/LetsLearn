using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Lấy thông tin user
        public async Task<UserDTO> GetByIdAsync(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("User not found.");

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
                IsVerified = user.IsVerified ? "true" : "false"
            };
        }

        // 🔹 Cập nhật thông tin user
        public async Task<UserDTO> UpdateAsync(Guid id, UpdateUserDTO dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(dto.Username))
                user.Username = dto.Username;
            if (!string.IsNullOrWhiteSpace(dto.Avatar))
                user.Avatar = dto.Avatar;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.CommitAsync();

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
                IsVerified = user.IsVerified ? "true" : "false"
            };
        }

        // Lấy tất cả user trừ bản thân
        public async Task<List<UserDTO>> GetAllAsync(Guid requesterId)
        {
            var users = await _unitOfWork.Users.GetAllAsync();

            return users
                .Where(u => u.Id != requesterId)
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    Avatar = u.Avatar,
                    Role = u.Role,
                    IsVerified = u.IsVerified ? "true" : "false"
                })
                .ToList();
        }

        // 🔹 Đổi mật khẩu
        public async Task UpdatePasswordAsync(Guid userId, UpdatePasswordDTO dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!VerifyPassword(dto.OldPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Old password is incorrect.");

            user.PasswordHash = HashPassword(dto.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CommitAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        // Lấy công việc của user (topics, bài học...)
        //public async Task<List<TopicDTO>> GetUserWorksAsync(Guid userId, string? type, Guid? courseId, DateTime? start, DateTime? end)
        //{
        //    var user = await _unitOfWork.Users.GetUserWithCoursesAsync(userId)
        //        ?? throw new KeyNotFoundException("User not found.");

        //    var topics = new List<TopicDTO>();

        //    foreach (var enrollment in user.EnrollmentDetails)
        //    {
        //        var course = enrollment.Course;
        //        if (course == null) continue;

        //        foreach (var section in course.Sections)
        //        {
        //            foreach (var topic in section.Topics)
        //            {
        //                if ((type == null || topic.Type == type) &&
        //                    (!courseId.HasValue || course.Id == courseId) &&
        //                    (!start.HasValue || topic.CreatedAt >= start) &&
        //                    (!end.HasValue || topic.CreatedAt <= end))
        //                {
        //                    topics.Add(new TopicDTO
        //                    {
        //                        Id = topic.Id,
        //                        SectionId = section.Id,
        //                        Title = topic.Title,
        //                        Type = topic.Type,
        //                        Course = new CourseDTO
        //                        {
        //                            Id = course.Id,
        //                            Title = course.Title,
        //                            Description = course.Description,
        //                            ImageUrl = course.ImageUrl
        //                        }
        //                    });
        //                }
        //            }
        //        }
        //    }

        //    return topics;
        //}

        // Rời khỏi khóa học
        //public async Task LeaveCourseAsync(Guid userId, Guid courseId)
        //{
        //    await _unitOfWork.EnrollmentDetails.DeleteByStudentIdAndCourseIdAsync(userId, courseId);
        //    await _unitOfWork.SaveChangesAsync();
        //}
    }
}
