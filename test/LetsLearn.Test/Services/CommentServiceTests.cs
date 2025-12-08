using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.CommentService;
using Moq;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class CommentServiceTests
    {
        // ------------------------------
        // 1. AddCommentAsync Tests
        // ------------------------------

        [Fact]
        public async Task AddCommentAsync_UserNotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(userRepo.Object);

            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((User)null!);

            var svc = new CommentService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.AddCommentAsync(Guid.NewGuid(),
                    new CreateCommentRequest { TopicId = Guid.NewGuid(), Text = "Hello" })
            );
        }

        [Fact]
        public async Task AddCommentAsync_TopicNotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            var topicRepo = new Mock<ITopicRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Topics).Returns(topicRepo.Object);

            // User tồn tại
            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User());

            // Topic null
            topicRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Topic)null!);

            var svc = new CommentService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.AddCommentAsync(Guid.NewGuid(),
                    new CreateCommentRequest { TopicId = Guid.NewGuid(), Text = "Hello" })
            );
        }

        [Fact]
        public async Task AddCommentAsync_ValidRequest_AddsCommentAndCommit()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            var topicRepo = new Mock<ITopicRepository>();
            var commentRepo = new Mock<ICommentRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Topics).Returns(topicRepo.Object);
            uow.Setup(x => x.Comments).Returns(commentRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User());

            topicRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Topic { Id = Guid.NewGuid() });

            commentRepo.Setup(x => x.AddAsync(It.IsAny<Comment>()))
                       .Returns(Task.CompletedTask);

            var svc = new CommentService(uow.Object);

            var req = new CreateCommentRequest { TopicId = Guid.NewGuid(), Text = "Hello" };

            await svc.AddCommentAsync(Guid.NewGuid(), req);

            commentRepo.Verify(x => x.AddAsync(It.IsAny<Comment>()), Times.Once);
            uow.Verify(x => x.CommitAsync(), Times.Once);
        }


        // ------------------------------
        // 2. GetCommentsByTopicAsync
        // ------------------------------

        [Fact]
        public async Task GetCommentsByTopicAsync_ReturnsMappedList()
        {
            var uow = new Mock<IUnitOfWork>();
            var commentRepo = new Mock<ICommentRepository>();

            uow.Setup(x => x.Comments).Returns(commentRepo.Object);

            var topicId = Guid.NewGuid();

            commentRepo.Setup(x => x.FindByTopicIdAsync(topicId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<Comment>
                       {
                           new Comment { Id = Guid.NewGuid(), Text = "C1", TopicId = topicId, UserId = Guid.NewGuid() }
                       });

            var svc = new CommentService(uow.Object);

            var list = await svc.GetCommentsByTopicAsync(topicId);

            Assert.Single(list);
            Assert.Equal("C1", list.First().Text);
        }


        // ------------------------------
        // 3. DeleteCommentAsync
        // ------------------------------

        [Fact]
        public async Task DeleteCommentAsync_NotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var commentRepo = new Mock<ICommentRepository>();

            uow.Setup(x => x.Comments).Returns(commentRepo.Object);

            commentRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Comment, bool>>>(),
                                                          It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Comment)null!);

            var svc = new CommentService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.DeleteCommentAsync(Guid.NewGuid())
            );
        }

        [Fact]
        public async Task DeleteCommentAsync_Found_DeletesAndCommits()
        {
            var uow = new Mock<IUnitOfWork>();
            var commentRepo = new Mock<ICommentRepository>();

            uow.Setup(x => x.Comments).Returns(commentRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var comment = new Comment { Id = Guid.NewGuid() };

            commentRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Comment, bool>>>(),
                                                          It.IsAny<CancellationToken>()))
                       .ReturnsAsync(comment);

            commentRepo.Setup(x => x.DeleteAsync(comment)).Returns(Task.CompletedTask);

            var svc = new CommentService(uow.Object);

            await svc.DeleteCommentAsync(comment.Id);

            commentRepo.Verify(x => x.DeleteAsync(comment), Times.Once);
            uow.Verify(x => x.CommitAsync(), Times.Once);
        }
    }
}
