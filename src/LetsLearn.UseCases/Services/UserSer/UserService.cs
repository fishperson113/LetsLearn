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

        public async Task<GetUserResponse> GetByIdAsync(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("User not found.");

            return new GetUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
            };
        }

        public async Task<UpdateUserResponse> UpdateAsync(Guid id, UpdateUserDTO dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(dto.Username))
                user.Username = dto.Username.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Avatar))
                user.Avatar = dto.Avatar.Trim();

            await _unitOfWork.CommitAsync();

            return new UpdateUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
            };
        }

        public async Task<List<GetUserResponse>> GetAllAsync(Guid requesterId)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Id != requesterId);

            return users
                .Select(u => new GetUserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    Avatar = u.Avatar,
                    Role = u.Role,
                })
                .ToList();
        }

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

        //public async Task LeaveCourseAsync(Guid userId, Guid courseId)
        //{
        //    await _unitOfWork.EnrollmentDetails.DeleteByStudentIdAndCourseIdAsync(userId, courseId);
        //    await _unitOfWork.SaveChangesAsync();
        //}
    }
}
