using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services;
using LetsLearn.UseCases.Services.CourseSer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.Test.Integration
{
    public class CourseServiceIntegrationTests : IntegrationTestBase
    {
        protected override void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ITopicService, TopicService>();
        }

        private async Task SeedBaseData(LetsLearnContext ctx)
        {
            var creator = new User
            {
                Id = Guid.NewGuid(),
                Email = "creator@test.com",
                PasswordHash = "hash",
                Role = "INSTRUCTOR",
                Username = "creator"
            };

            var student = new User
            {
                Id = Guid.NewGuid(),
                Email = "student@test.com",
                PasswordHash = "hash",
                Role = "STUDENT",
                Username = "student"
            };

            ctx.Users.AddRange(creator, student);

            var course = new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = creator.Id,
                TotalJoined = 2,
                Sections = new List<Section>()
            };

            ctx.Courses.Add(course);

            ctx.Enrollments.Add(new Enrollment
            {
                CourseId = "C1",
                StudentId = student.Id,
                JoinDate = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task GetCourseByIdAsync_NotFound_Throws()
        {
    await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetCourseByIdAsync("NOT_EXIST"));
        }

        [Fact]
        public async Task GetCourseByIdAsync_ReturnsCreatorAndStudents()
        {
            await using var scope = _provider.CreateAsyncScope();

            var ctx = scope.ServiceProvider.GetRequiredService<LetsLearnContext>();
            await SeedBaseData(ctx);

            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            var result = await service.GetCourseByIdAsync("C1");

            Assert.Equal("C1", result.Id);
            Assert.Equal("Course 1", result.Title);

            // Creator
            Assert.NotNull(result.Creator);
            Assert.Equal("creator", result.Creator.Username);

            // Students
            Assert.NotNull(result.Students);
            Assert.Single(result.Students);
            Assert.Equal("student", result.Students.First().Username);
        }

        [Fact]
        public async Task GetCourseByIdAsync_NoStudents_ReturnsEmptyList()
        {
            await using var scope = _provider.CreateAsyncScope();

            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            var creator = new User
            {
                Id = Guid.NewGuid(),
                Email = "creator@test.com",
                PasswordHash = "hashed-password",
                Role = "INSTRUCTOR",
                Username = "creator"
            };

            await uow.Users.AddAsync(creator);

            var course = new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = creator.Id,
                TotalJoined = 1,
                Sections = new List<Section>()
            };

            await uow.Course.AddAsync(course);
            await uow.CommitAsync();

            var result = await service.GetCourseByIdAsync("C1");

            Assert.NotNull(result);
            Assert.NotNull(result.Students);
            Assert.Empty(result.Students);
            Assert.NotNull(result.Creator);
            Assert.Equal(creator.Id, result.Creator.Id);
        }

        [Fact]
        public async Task CreateCourseAsync_Valid_CreatesCourseAndEnrollment()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "u@test.com",
                PasswordHash = "hash",
                Role = "INSTRUCTOR",
                Username = "creator"
            };

            await uow.Users.AddAsync(user);
            await uow.CommitAsync();

            var result = await service.CreateCourseAsync(new CreateCourseRequest
            {
                Id = "C1",
                Title = "Course 1"
            }, user.Id);

            var course = await uow.Course.GetByIdAsync("C1");
            var enrollments = await uow.Enrollments.GetByStudentId(user.Id);

            Assert.NotNull(course);
            Assert.Single(enrollments);
            Assert.Equal(user.Id, enrollments.First().StudentId);
        }

        [Fact]
        public async Task CreateCourseAsync_TitleEmpty_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateCourseAsync(new CreateCourseRequest
                {
                    Id = "C1",
                    Title = ""
                }, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateCourseAsync_TitleExists_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var userId = Guid.NewGuid();
            await uow.Users.AddAsync(new User
            {
                Id = userId,
                Email = "u@test.com",
                Username = "instructor1",
                PasswordHash = "hash",
                Role = "INSTRUCTOR"
            });

            await uow.Course.AddAsync(new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = userId,
                IsPublished = true
            });

            await uow.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCourseAsync(new CreateCourseRequest
                {
                    Id = "C2",
                    Title = "Course 1"
                }, userId));
        }

        [Fact]
        public async Task UpdateCourseAsync_Valid_UpdatesCourse()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var course = new Course { Id = "C1", Title = "Old" };
            await uow.Course.AddAsync(course);
            await uow.CommitAsync();

            var result = await service.UpdateCourseAsync(new UpdateCourseRequest
            {
                Id = "C1",
                Title = "New"
            });

            Assert.Equal("New", result.Title);
        }

        [Fact]
        public async Task UpdateCourseAsync_NotFound_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateCourseAsync(new UpdateCourseRequest { Id = "NOPE" }));
        }

        [Fact]
        public async Task GetAllCoursesAsync_ReturnsOnlyPublishedCourses()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await uow.Course.AddAsync(new Course { Id = "C1", Title = "A", IsPublished = true });
            await uow.Course.AddAsync(new Course { Id = "C2", Title = "B", IsPublished = false });
            await uow.CommitAsync();

            var list = await service.GetAllCoursesAsync();

            Assert.Single(list);
            Assert.Equal("C1", list.First().Id);
        }

        [Fact]
        public async Task GetAllCoursesByUserIdAsync_UserNotFound_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetAllCoursesByUserIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAllCoursesByUserIdAsync_ReturnsEnrolledCourses()
        {
            await using var scope = _provider.CreateAsyncScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "student@test.com",
                Username = "student",
                PasswordHash = "hash",
                Role = "STUDENT"
            };

            var course = new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = Guid.NewGuid(),
                IsPublished = true
            };

            await uow.Users.AddAsync(user);
            await uow.Course.AddAsync(course);
            await uow.Enrollments.AddAsync(new Enrollment
            {
                CourseId = "C1",
                StudentId = user.Id,
                JoinDate = DateTime.UtcNow
            });

            await uow.CommitAsync();

            var result = await service.GetAllCoursesByUserIdAsync(user.Id);

            Assert.Single(result);
            Assert.Equal("C1", result.First().Id);
        }

        [Fact]
        public async Task AddUserToCourseAsync_Success_AddsEnrollment()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "u@test.com",
                Username = "student1",
                PasswordHash = "hashed",
                Role = "STUDENT"
            };

            var course = new Course
            {
                Id = "C1",
                Title = "Test Course",   
                CreatorId = Guid.NewGuid(),
                TotalJoined = 1,
                IsPublished = true,       
                Sections = new List<Section>()
            };

            await uow.Users.AddAsync(user);
            await uow.Course.AddAsync(course);
            await uow.CommitAsync();

            await service.AddUserToCourseAsync("C1", user.Id);

            var enrollments = await uow.Enrollments.GetByStudentId(user.Id);
            Assert.Single(enrollments);
        }

        [Fact]
        public async Task AddUserToCourseAsync_CourseNotFound_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "u@test.com",
                Username = "student",
                PasswordHash = "hash",
                Role = "STUDENT"
            };

            await uow.Users.AddAsync(user);
            await uow.CommitAsync();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AddUserToCourseAsync("NOT_EXIST", user.Id));
        }

        [Fact]
        public async Task AddUserToCourseAsync_UserNotFound_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var course = new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = Guid.NewGuid()
            };

            await uow.Course.AddAsync(course);
            await uow.CommitAsync();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AddUserToCourseAsync("C1", Guid.NewGuid()));
        }

        [Fact]
        public async Task AddUserToCourseAsync_AlreadyEnrolled_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ICourseService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "u@test.com",
                Username = "student",
                PasswordHash = "hash",
                Role = "STUDENT"
            };

            var course = new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = Guid.NewGuid(),
                TotalJoined = 1
            };

            await uow.Users.AddAsync(user);
            await uow.Course.AddAsync(course);
            await uow.Enrollments.AddAsync(new Enrollment
            {
                CourseId = "C1",
                StudentId = user.Id,
                JoinDate = DateTime.UtcNow
            });

            await uow.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddUserToCourseAsync("C1", user.Id));
        }
    }
}
