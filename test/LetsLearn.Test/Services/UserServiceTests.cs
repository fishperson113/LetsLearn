using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.UserSer;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class UserServiceTests
    {
        // ------------------------------------------------------
        // 1. GetByIdAsync
        // ------------------------------------------------------

        [Fact]
        public async Task GetByIdAsync_UserNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            uow.Setup(x => x.Users).Returns(repo.Object);
            repo.Setup(x => x.GetByIdWithEnrollmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null!);

            var service = new UserService(uow.Object, null!, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetByIdAsync(Guid.NewGuid()));
        }


        [Fact]
        public async Task GetByIdAsync_ReturnsUserWithEnrollments()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Username = "tester",
                Enrollments = new List<Enrollment>
                {
                    new Enrollment { CourseId = "c1", StudentId = Guid.NewGuid() }
                }
            };

            uow.Setup(x => x.Users).Returns(repo.Object);
            repo.Setup(x => x.GetByIdWithEnrollmentsAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var service = new UserService(uow.Object, null!, null!);

            var result = await service.GetByIdAsync(user.Id);

            Assert.Equal(user.Id, result.Id);
            Assert.Single(result.Enrollments);
        }


        // ------------------------------------------------------
        // 2. UpdateAsync
        // ------------------------------------------------------

        [Fact]
        public async Task UpdateAsync_UserNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            uow.Setup(x => x.Users).Returns(repo.Object);
            repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null!);

            var service = new UserService(uow.Object, null!, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateAsync(Guid.NewGuid(), new UpdateUserDTO()));
        }


        [Fact]
        public async Task UpdateAsync_UpdatesUsername()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            var user = new User { Id = Guid.NewGuid(), Username = "old" };

            uow.Setup(x => x.Users).Returns(repo.Object);
            repo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var service = new UserService(uow.Object, null!, null!);

            var dto = new UpdateUserDTO { Username = "newname" };
            var result = await service.UpdateAsync(user.Id, dto);

            Assert.Equal("newname", result.Username);
        }


        [Fact]
        public async Task UpdateAsync_UpdatesAvatar()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            var user = new User { Id = Guid.NewGuid(), Avatar = "old.png" };

            uow.Setup(x => x.Users).Returns(repo.Object);
            repo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var service = new UserService(uow.Object, null!, null!);

            var dto = new UpdateUserDTO { Avatar = "new.png" };
            var result = await service.UpdateAsync(user.Id, dto);

            Assert.Equal("new.png", result.Avatar);
        }


        [Fact]
        public async Task UpdateAsync_CommitFails_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            var user = new User { Id = Guid.NewGuid() };

            uow.Setup(x => x.Users).Returns(repo.Object);
            repo.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            uow.Setup(x => x.CommitAsync()).ThrowsAsync(new DbUpdateException());

            var service = new UserService(uow.Object, null!, null!);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateAsync(user.Id, new UpdateUserDTO()));
        }


        // ------------------------------------------------------
        // 3. GetAllAsync
        // ------------------------------------------------------

        [Fact]
        public async Task GetAllAsync_ReturnsUsersExceptRequester()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IUserRepository>();

            var requesterId = Guid.NewGuid();

            uow.Setup(x => x.Users).Returns(repo.Object);

            repo.Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "u1" },
                new User { Id = Guid.NewGuid(), Username = "u2" }
            });

            var service = new UserService(uow.Object, null!, null!);

            var result = await service.GetAllAsync(requesterId);

            Assert.Equal(2, result.Count());
        }


        // ------------------------------------------------------
        // 4. LeaveCourseAsync
        // ------------------------------------------------------

        [Fact]
        public async Task LeaveCourseAsync_EnrollmentNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var enrollRepo = new Mock<IEnrollmentRepository>();

            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);
            enrollRepo.Setup(x => x.GetByIdsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Enrollment)null!);

            var service = new UserService(uow.Object, null!, null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.LeaveCourseAsync(Guid.NewGuid(), "course1"));
        }


        [Fact]
        public async Task LeaveCourseAsync_DecreasesCourseTotalJoined()
        {
            var uow = new Mock<IUnitOfWork>();
            var enrollRepo = new Mock<IEnrollmentRepository>();
            var courseRepo = new Mock<ICourseRepository>();

            var enrollment = new Enrollment { StudentId = Guid.NewGuid(), CourseId = "c1" };
            var course = new Course { Id = "c1", TotalJoined = 5, Sections = new List<Section>() };

            uow.Setup(x => x.Enrollments).Returns(enrollRepo.Object);
            uow.Setup(x => x.Course).Returns(courseRepo.Object);

            enrollRepo.Setup(x => x.GetByIdsAsync(enrollment.StudentId, enrollment.CourseId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(enrollment);

            courseRepo.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(course);

            enrollRepo.Setup(x => x.DeleteByStudentIdAndCourseIdAsync(enrollment.StudentId, enrollment.CourseId, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var service = new UserService(uow.Object, null!, null!);

            await service.LeaveCourseAsync(enrollment.StudentId, enrollment.CourseId);

            Assert.Equal(4, course.TotalJoined);
        }
    }
}
