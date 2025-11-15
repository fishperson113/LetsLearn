using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.UserSer
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITopicService _topicService;
        private readonly ICourseService _courseService;
        public UserService(IUnitOfWork unitOfWork, ITopicService topicService, ICourseService courseService)
        {
            _unitOfWork = unitOfWork;
            _topicService = topicService;
            _courseService = courseService;
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Null-coalesce throw when user not found: +1
        // D = 1 => Minimum Test Cases = D + 1 = 2
        public async Task<GetUserResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _unitOfWork.Users.GetByIdWithEnrollmentsAsync(id, ct)
                ?? throw new KeyNotFoundException("User not found.");

            return new GetUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
                Enrollments = user.Enrollments?
                                              .Select(e => new EnrollmentDTO
                                              {
                                                  StudentId = e.StudentId,
                                                  CourseId = e.CourseId,
                                                  JoinDate = e.JoinDate
                                              })
                                              .ToList()
            };
        }

        // Test Case Estimation:
        // Decision points (D):
        // - Null-coalesce throw when user not found: +1
        // - if Username provided: +1
        // - if Avatar provided: +1
        // D = 3 => Minimum Test Cases = D + 1 = 4
        public async Task<UpdateUserResponse> UpdateAsync(Guid id, UpdateUserDTO dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(dto.Username))
                user.Username = dto.Username.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Avatar))
                user.Avatar = dto.Avatar.Trim();

            try
            {
                await _unitOfWork.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist user changes.", ex);
            }

            return new UpdateUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Avatar = user.Avatar,
                Role = user.Role,
            };
        }

        // Test Case Estimation:
        // Decision points (D):
        // - No branching here: +0
        // D = 0 => Minimum Test Cases = D + 1 = 1
        public async Task<IEnumerable<GetUserResponse>> GetAllAsync(Guid requesterId)
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

        public async Task<IEnumerable<TopicDTO>> GetAllWorksOfUserAsync(Guid userId, string? type, DateTime? start, DateTime? end, CancellationToken ct = default)
        {
            if (start.HasValue && end.HasValue && start > end)
                throw new ArgumentException("Start time must be after end time");

            start ??= DateTime.MinValue;
            end ??= DateTime.MaxValue;

            var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
            if (user == null)
                throw new ArgumentException("User not found");

            var courses = new List<Course>();

            if (user.Role.Equals("TEACHER", StringComparison.OrdinalIgnoreCase))
            {
                courses = (await _unitOfWork.Course.GetByCreatorId(user.Id, ct)).Where(c => c != null).Select(c => c!).ToList();
            }
            else
            {
                var enrollments = await _unitOfWork.Enrollments.GetAllByStudentIdAsync(user.Id, ct);
                foreach (var enrollment in enrollments)
                {
                    var course = await _unitOfWork.Course.GetByIdAsync(enrollment.CourseId, ct);
                    if (course != null)
                    {
                        courses.Add(course);
                    }
                }
            }

            if (courses == null || !courses.Any())
                throw new Exception("No courses found for user");

            var result = new List<TopicDTO>();

            foreach (var course in courses)
            {
                foreach (var section in course.Sections)
                {
                    foreach (var topic in section.Topics)
                    {
                        if (string.IsNullOrEmpty(type) || type.Equals(topic.Type, StringComparison.OrdinalIgnoreCase))
                        {
                            object? topicData = await _courseService.GetTopicDataByTypeAsync(topic.Id, userId, start, end, ct);
                            if (topicData != null)
                            {
                                var topicDTO = ToDTO(topic);
                                topicDTO.Data = topicData;
                                //topicDTO.Data = topicData.Item;
                                //topicDTO.Response = topicData.Response;
                                result.Add(topicDTO);
                            }
                        }
                    }
                }
            }

            return result;
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

        public async Task LeaveCourseAsync(Guid userId, string courseId, CancellationToken ct = default)
        {
            // Kiểm tra xem enrollment có tồn tại
            var enrollment = await _unitOfWork.Enrollments.GetByIdsAsync(userId, courseId, ct);
            if (enrollment == null)
                throw new KeyNotFoundException("Enrollment not found for this user and course.");

            // Xóa enrollment
            await _unitOfWork.Enrollments.DeleteByStudentIdAndCourseIdAsync(userId, courseId, ct);

            // Cập nhật tổng số học viên của khóa học
            var course = await _unitOfWork.Course.GetByIdAsync(courseId, ct);
            if (course != null && course.TotalJoined > 0)
            {
                course.TotalJoined -= 1;
            }

            // Commit transaction
            await _unitOfWork.CommitAsync();
        }
    }
}
