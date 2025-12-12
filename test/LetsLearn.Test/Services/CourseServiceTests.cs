using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.CourseSer;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace LetsLearn.Test.Services
{
    public class CourseServiceTests
    {
        // ============================================================
        // 1. CreateCourseAsync
        // ============================================================

        [Fact]
        public async Task CreateCourseAsync_TitleNull_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var service = new CourseService(uow.Object, null!);

            var dto = new CreateCourseRequest
            {
                Id = "C1",
                Title = " "
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateCourseAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateCourseAsync_TitleExists_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            courseRepo.Setup(x => x.ExistByTitle("Test")).ReturnsAsync(true);

            var service = new CourseService(uow.Object, null!);

            var dto = new CreateCourseRequest { Title = "Test", Id = "course1" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCourseAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateCourseAsync_IdExists_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            courseRepo.Setup(x => x.ExistByTitle(It.IsAny<string>()))
                .ReturnsAsync(false);

            courseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Course, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = new CourseService(uow.Object, null!);

            var dto = new CreateCourseRequest { Title = "Valid", Id = "CID1" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCourseAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateCourseAsync_CommitFails_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var enrollRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);

            courseRepo.Setup(x => x.ExistByTitle(It.IsAny<string>())).ReturnsAsync(false);
            courseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Course, bool>>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

            enrollRepo.Setup(x => x.AddAsync(It.IsAny<Enrollment>()))
                      .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync()).ThrowsAsync(new DbUpdateException());

            var service = new CourseService(uow.Object, null!);

            var dto = new CreateCourseRequest { Title = "Course X", Id = "ID1" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCourseAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateCourseAsync_Success_ReturnsResponse()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var enrollRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);

            courseRepo.Setup(x => x.ExistByTitle(It.IsAny<string>())).ReturnsAsync(false);
            courseRepo.Setup(x => x.ExistsAsync(
                It.IsAny<Expression<Func<Course, bool>>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(false);

            courseRepo.Setup(x => x.AddAsync(It.IsAny<Course>()))
                      .Returns(Task.CompletedTask);

            enrollRepo.Setup(x => x.AddAsync(It.IsAny<Enrollment>()))
                      .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var service = new CourseService(uow.Object, null!);

            var dto = new CreateCourseRequest { Title = "Course ABC", Id = "C1" };

            var result = await service.CreateCourseAsync(dto, Guid.NewGuid());

            Assert.Equal("Course ABC", result.Title);
        }

        // ============================================================
        // 2. UpdateCourseAsync
        // ============================================================

        [Fact]
        public async Task UpdateCourseAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("X", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Course)null!);

            var service = new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateCourseAsync(new UpdateCourseRequest { Id = "X" }));
        }


        [Fact]
        public async Task UpdateCourseAsync_UpdateTitle()
        {
            var course = new Course { Id = "C1", Title = "Old" };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var service = new CourseService(uow.Object, null!);

            var dto = new UpdateCourseRequest { Id = "C1", Title = "New Title" };

            var result = await service.UpdateCourseAsync(dto);

            Assert.Equal("New Title", result.Title);
        }

        [Fact]
        public async Task UpdateCourseAsync_CommitFails_Throws()
        {
            var course = new Course { Id = "C1", Title = "Old" };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            uow.Setup(x => x.CommitAsync()).ThrowsAsync(new DbUpdateException());

            var service = new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateCourseAsync(new UpdateCourseRequest { Id = "C1" }));
        }

        // ============================================================
        // 3. GetCourseByIdAsync
        // ============================================================

        [Fact]
        public async Task GetCourseByIdAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("X", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Course)null!);

            var service = new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetCourseByIdAsync("X"));
        }


        [Fact]
        public async Task GetCourseByIdAsync_ReturnsCourse()
        {
            var course = new Course
            {
                Id = "C1",
                Title = "Course 1",
                CreatorId = Guid.NewGuid(),
                Sections = new List<Section>()
            };

            var creator = new User
            {
                Id = course.CreatorId,
                Username = "teacher",
                Avatar = "avatar.png"
            };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var userRepo = new Mock<IUserRepository>();
            var enrollmentRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Enrollments).Returns(enrollmentRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            userRepo.Setup(x => x.GetByIdAsync(course.CreatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);

            enrollmentRepo.Setup(x => x.FindAsync(
                    It.IsAny<Expression<Func<Enrollment, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enrollment>()); // no students

            var service = new CourseService(uow.Object, null!);

            var result = await service.GetCourseByIdAsync("C1");

            Assert.Equal("C1", result.Id);
            Assert.NotNull(result.Creator);
            Assert.Equal("teacher", result.Creator.Username);
            Assert.NotNull(result.Students);
            Assert.Empty(result.Students);
        }

        // ============================================================
        // 4. GetAllCoursesAsync
        // ============================================================

        [Fact]
        public async Task GetAllCoursesAsync_ReturnsPublished()
        {
            var courses = new List<Course>
            {
                new Course { Id = "C1", Title = "A", IsPublished = true },
                new Course { Id = "C2", Title = "B", IsPublished = true },
            };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            courseRepo.Setup(x => x.GetAllCoursesByIsPublishedTrue())
                      .ReturnsAsync(courses);

            var service = new CourseService(uow.Object, null!);

            var result = await service.GetAllCoursesAsync();

            Assert.Equal(2, result.Count());
        }

        // ============================================================
        // 5. AddUserToCourseAsync
        // ============================================================

        [Fact]
        public async Task AddUserToCourseAsync_CourseNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            courseRepo.Setup(x => x.GetByIdAsync("X", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Course)null!);

            var service = new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AddUserToCourseAsync("X", Guid.NewGuid()));
        }

        [Fact]
        public async Task AddUserToCourseAsync_UserNotFound_Throws()
        {
            var course = new Course { Id = "C1", TotalJoined = 10 };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var userRepo = new Mock<IUserRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((User)null!);

            var service = new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AddUserToCourseAsync("C1", Guid.NewGuid()));
        }

        [Fact]
        public async Task AddUserToCourseAsync_EnrollmentExists_Throws()
        {
            var course = new Course { Id = "C1", TotalJoined = 10 };
            var user = new User { Id = Guid.NewGuid() };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var userRepo = new Mock<IUserRepository>();
            var enrollRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

            enrollRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

            var service = new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddUserToCourseAsync("C1", user.Id));
        }

        [Fact]
        public async Task AddUserToCourseAsync_CommitFails_Throws()
        {
            var course = new Course { Id = "C1", TotalJoined = 10 };
            var user = new User { Id = Guid.NewGuid() };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var userRepo = new Mock<IUserRepository>();
            var enrollRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

            enrollRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

            enrollRepo.Setup(x => x.AddAsync(It.IsAny<Enrollment>()))
                      .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync()).ThrowsAsync(new DbUpdateException());

            var service =
                new CourseService(uow.Object, null!);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddUserToCourseAsync("C1", user.Id));
        }

        [Fact]
        public async Task AddUserToCourseAsync_Success()
        {
            var course = new Course { Id = "C1", TotalJoined = 10 };
            var user = new User { Id = Guid.NewGuid() };

            var uow = new Mock<IUnitOfWork>();
            var courseRepo = new Mock<ICourseRepository>();
            var userRepo = new Mock<IUserRepository>();
            var enrollRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Course).Returns(courseRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);

            courseRepo.Setup(x => x.GetByIdAsync("C1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            userRepo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

            enrollRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

            enrollRepo.Setup(x => x.AddAsync(It.IsAny<Enrollment>()))
                      .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var service = new CourseService(uow.Object, null!);

            await service.AddUserToCourseAsync("C1", user.Id);

            Assert.Equal(11, course.TotalJoined);
        }
    }
}
