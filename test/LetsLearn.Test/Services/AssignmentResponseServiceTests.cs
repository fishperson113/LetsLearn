using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.AssignmentResponseService;
using Moq;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class AssignmentResponseServiceTests
    {
        // ---------------------------------------
        // 1. GetAssigmentResponseByIdAsync
        // ---------------------------------------
        [Fact]
        public async Task GetAssignmentResponseByIdAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);
            repo.Setup(x => x.GetByIdWithFilesAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AssignmentResponse)null!);

            var svc = new AssignmentResponseService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.GetAssigmentResponseByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAssignmentResponseByIdAsync_Found_ReturnsDto()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();

            var entity = new AssignmentResponse
            {
                Id = Guid.NewGuid(),
                TopicId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SubmittedAt = DateTime.UtcNow,
                Files = new List<CloudinaryFile>()
            };

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);
            repo.Setup(x => x.GetByIdWithFilesAsync(entity.Id))
                .ReturnsAsync(entity);

            var svc = new AssignmentResponseService(uow.Object);

            var dto = await svc.GetAssigmentResponseByIdAsync(entity.Id);

            Assert.Equal(entity.Id, dto.Id);
        }


        // ---------------------------------------
        // 2. CreateAssigmentResponseAsync
        // ---------------------------------------
        [Fact]
        public async Task CreateAssignmentResponseAsync_CreatesAndAddsFiles_ReturnsDto()
        {
            // Arrange
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();
            var fileRepo = new Mock<IRepository<CloudinaryFile>>();

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);
            uow.Setup(x => x.CloudinaryFiles).Returns(fileRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            repo.Setup(x => x.AddAsync(It.IsAny<AssignmentResponse>()))
                .Returns(Task.CompletedTask);

            fileRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<CloudinaryFile>>()))
                .Returns(Task.CompletedTask);

            var service = new AssignmentResponseService(uow.Object);

            var request = new CreateAssignmentResponseRequest
            {
                TopicId = Guid.NewGuid(),
                Data = new AssignmentResponseData
                {
                    SubmittedAt = DateTime.UtcNow,
                    Note = "Test note",
                    Mark = 8,
                    Files = new List<CloudinaryFile>
            {
                new CloudinaryFile
                {
                    Name = "file1.png",
                    DisplayUrl = "http://img.com/1",
                    DownloadUrl = "http://dl.com/1"
                }
            }
                }
            };

            // Act
            var result = await service.CreateAssigmentResponseAsync(request, Guid.NewGuid());

            // Assert
            Assert.Equal(request.TopicId, result.TopicId);
            Assert.Single(result.Data.Files);
            Assert.Equal("file1.png", result.Data.Files.First().Name);
        }


        // ---------------------------------------
        // 3. GetAllAssigmentResponseByTopicIdAsync
        // ---------------------------------------
        [Fact]
        public async Task GetAllAssignmentResponseByTopicIdAsync_ReturnsList()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);

            repo.Setup(x => x.GetAllByTopicIdWithFilesAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<AssignmentResponse>
                {
                    new AssignmentResponse { Id = Guid.NewGuid(), Files = new List<CloudinaryFile>() }
                });

            var svc = new AssignmentResponseService(uow.Object);

            var list = await svc.GetAllAssigmentResponseByTopicIdAsync(Guid.NewGuid());

            Assert.Single(list);
        }


        // ---------------------------------------
        // 4. UpdateAssigmentResponseByIdAsync
        // ---------------------------------------
        [Fact]
        public async Task UpdateAssignmentResponseAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);
            repo.Setup(x => x.GetByIdTrackedWithFilesAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AssignmentResponse)null!);

            var svc = new AssignmentResponseService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.UpdateAssigmentResponseByIdAsync(Guid.NewGuid(), new UpdateAssignmentResponseRequest()));
        }

        [Fact]
        public async Task UpdateAssignmentResponseAsync_UpdatesAndReplacesFiles()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();
            var fileRepo = new Mock<IRepository<CloudinaryFile>>();

            var entity = new AssignmentResponse
            {
                Id = Guid.NewGuid(),
                TopicId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Files = new List<CloudinaryFile> { new CloudinaryFile { Id = Guid.NewGuid() } }
            };

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);
            uow.Setup(x => x.CloudinaryFiles).Returns(fileRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            repo.Setup(x => x.GetByIdTrackedWithFilesAsync(entity.Id))
                .ReturnsAsync(entity);

            fileRepo.Setup(x => x.DeleteRangeAsync(entity.Files))
                    .Returns(Task.CompletedTask);

            fileRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<CloudinaryFile>>()))
                    .Returns(Task.CompletedTask);

            var svc = new AssignmentResponseService(uow.Object);

            var dto = new UpdateAssignmentResponseRequest
            {
                TopicId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Data = new AssignmentResponseData
                {
                    SubmittedAt = DateTime.UtcNow,
                    Note = "Updated",
                    Mark = 10,
                    Files = new List<CloudinaryFile>
                    {
                        new CloudinaryFile { Name = "new.png", DisplayUrl = "http://img.com/new" }
                    }
                }
            };

            var result = await svc.UpdateAssigmentResponseByIdAsync(entity.Id, dto);

            Assert.Equal(dto.TopicId, result.TopicId);
            Assert.Equal("Updated", result.Data.Note);
            Assert.Single(result.Data.Files);
        }


        // ---------------------------------------
        // 5. DeleteAssigmentResponseAsync
        // ---------------------------------------
        [Fact]
        public async Task DeleteAssignmentResponseAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);

            repo.Setup(x => x.GetByIdTrackedWithFilesAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AssignmentResponse)null!);

            var svc = new AssignmentResponseService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.DeleteAssigmentResponseAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteAssignmentResponseAsync_DeletesAndCommits()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IAssignmentResponseRepository>();

            var entity = new AssignmentResponse { Id = Guid.NewGuid() };

            uow.Setup(x => x.AssignmentResponses).Returns(repo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            repo.Setup(x => x.GetByIdTrackedWithFilesAsync(entity.Id))
                .ReturnsAsync(entity);

            repo.Setup(x => x.DeleteAsync(entity)).Returns(Task.CompletedTask);

            var svc = new AssignmentResponseService(uow.Object);

            await svc.DeleteAssigmentResponseAsync(entity.Id);

            repo.Verify(x => x.DeleteAsync(entity), Times.Once);
            uow.Verify(x => x.CommitAsync(), Times.Once);
        }
    }
}
