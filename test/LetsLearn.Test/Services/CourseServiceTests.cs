using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.CourseSer;
using System.Threading;
using System.Linq.Expressions;

namespace LetsLearn.Test.Services
{
    public class CourseServiceTests
    {
        [Fact]
        public async Task CreateAsync_TitleMissing_ThrowsArgumentException()
        {
            var svc = new CourseService(new Mock<IUnitOfWork>().Object);
            var dto = new CreateCourseRequest { Title = " " };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateAsync_Valid_SavesAndReturns()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            repo.Setup(x => x.ExistByTitle("T")).ReturnsAsync(false);
            repo.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            repo.Setup(x => x.AddAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new CourseService(uow.Object);
            var dto = new CreateCourseRequest { Id = "c1", Title = "T" };

            var resp = await svc.CreateAsync(dto, Guid.NewGuid());

            Assert.Equal("c1", resp.Id);
            uow.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_TitleExists_ThrowsInvalidOperation()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            repo.Setup(x => x.ExistByTitle("T")).ReturnsAsync(true);

            var svc = new CourseService(uow.Object);
            var dto = new CreateCourseRequest { Id = "c1", Title = "T" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateAsync_IdExists_ThrowsInvalidOperation()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            repo.Setup(x => x.ExistByTitle("T")).ReturnsAsync(false);
            repo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Course, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var svc = new CourseService(uow.Object);
            var dto = new CreateCourseRequest { Id = "c1", Title = "T" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsKeyNotFound()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            repo.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync((Course)null!);

            var svc = new CourseService(uow.Object);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateAsync(new UpdateCourseRequest { Id = "c1" }));
        }

        [Fact]
        public async Task UpdateAsync_TitleProvidedUnique_Updates()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            var ent = new Course { Id = "c1", Title = "Old" };
            repo.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(ent);
            repo.Setup(x => x.ExistByTitle("New")).ReturnsAsync(false);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new CourseService(uow.Object);
            var resp = await svc.UpdateAsync(new UpdateCourseRequest { Id = "c1", Title = "New" });
            Assert.Equal("New", resp.Title);
        }

        [Fact]
        public async Task UpdateAsync_TitleProvidedExists_ThrowsInvalidOperation()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            var ent = new Course { Id = "c1", Title = "Old" };
            repo.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(ent);
            repo.Setup(x => x.ExistByTitle("New")).ReturnsAsync(true);

            var svc = new CourseService(uow.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(new UpdateCourseRequest { Id = "c1", Title = "New" }));
        }

        [Fact]
        public async Task UpdateAsync_NoTitle_UpdatesOtherFields()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            var ent = new Course { Id = "c1", Description = "old" };
            repo.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(ent);
            repo.Setup(x => x.ExistByTitle(It.IsAny<string>())).ReturnsAsync(false);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new CourseService(uow.Object);
            var resp = await svc.UpdateAsync(new UpdateCourseRequest { Id = "c1", Description = "new" });
            Assert.Equal("new", resp.Description);
        }

        [Fact]
        public async Task GetAllPublicAsync_ReturnsList()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(repo.Object);
            repo.Setup(x => x.GetAllCoursesByIsPublishedTrue())
                .ReturnsAsync(new List<Course?> { new Course { Id = "c1" } });

            var svc = new CourseService(uow.Object);
            var list = await svc.GetAllPublicAsync();
            Assert.Single(list);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_UserNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var course = new Mock<ICourseRepository>();
            var users = new Mock<IUserRepository>();
            uow.Setup(x => x.Course).Returns(course.Object);
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            var svc = new CourseService(uow.Object);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetAllByUserIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_UserFound_Returns()
        {
            var uow = new Mock<IUnitOfWork>();
            var course = new Mock<ICourseRepository>();
            var users = new Mock<IUserRepository>();
            uow.Setup(x => x.Course).Returns(course.Object);
            uow.Setup(x => x.Users).Returns(users.Object);
            users.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
            course.Setup(x => x.GetByCreatorId(It.IsAny<Guid>()))
                  .ReturnsAsync(new List<Course?> { new Course { Id = "c1" } });

            var svc = new CourseService(uow.Object);
            var list = await svc.GetAllByUserIdAsync(Guid.NewGuid());
            Assert.Single(list);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var course = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(course.Object);
            course.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync((Course)null!);

            var svc = new CourseService(uow.Object);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync("c1"));
        }

        [Fact]
        public async Task GetByIdAsync_Found_Returns()
        {
            var uow = new Mock<IUnitOfWork>();
            var course = new Mock<ICourseRepository>();
            uow.Setup(x => x.Course).Returns(course.Object);
            var ent = new Course { Id = "c1" };
            course.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(ent);

            var svc = new CourseService(uow.Object);
            var dto = await svc.GetByIdAsync("c1");
            Assert.Equal("c1", dto.Id);
        }
    }
}
