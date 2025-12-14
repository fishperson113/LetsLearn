using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class TopicServiceTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ILogger<TopicService>> _logger = new();
        private readonly TopicService _svc;

        public TopicServiceTests()
        {
            _svc = new TopicService(_uow.Object, _logger.Object);
        }

        private void SetupCommit()
        {
            _uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        }

        private void SetupAddTopic()
        {
            _uow.Setup(x => x.Topics.AddAsync(It.IsAny<Topic>()))
                .Returns(Task.CompletedTask);
        }

        #region ============================= CREATE TOPIC TESTS =============================

        [Fact]
        public async Task CreateTopicAsync_RequestNull_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _svc.CreateTopicAsync(null!));
        }

        [Fact]
        public async Task CreateTopicAsync_UnsupportedType_Throws()
        {
            var req = new CreateTopicRequest
            {
                Title = "ABC",
                Type = "unknown",
                SectionId = Guid.NewGuid()
            };

            SetupAddTopic();
            SetupCommit();

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                _svc.CreateTopicAsync(req));
        }

        [Fact]
        public async Task CreateTopicAsync_PageType_Success()
        {
            SetupAddTopic();
            SetupCommit();

            _uow.Setup(x => x.TopicPages.AddAsync(It.IsAny<TopicPage>()))
                .Returns(Task.CompletedTask);

            var req = new CreateTopicRequest
            {
                Title = "Page",
                Type = "page",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicPageRequest
                {
                    Description = "desc",
                    Content = "content"
                })
            };

            var res = await _svc.CreateTopicAsync(req);

            Assert.IsType<TopicPage>(res.Data);
        }

        [Fact]
        public async Task CreateTopicAsync_FileType_Success()
        {
            SetupAddTopic();
            SetupCommit();

            _uow.Setup(x => x.TopicFiles.AddAsync(It.IsAny<TopicFile>()))
                .Returns(Task.CompletedTask);

            var req = new CreateTopicRequest
            {
                Title = "File",
                Type = "file",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicFileRequest { Description = "file desc" })
            };

            var res = await _svc.CreateTopicAsync(req);

            Assert.IsType<TopicFile>(res.Data);
        }

        [Fact]
        public async Task CreateTopicAsync_LinkType_Success()
        {
            SetupAddTopic();
            SetupCommit();

            _uow.Setup(x => x.TopicLinks.AddAsync(It.IsAny<TopicLink>()))
                .Returns(Task.CompletedTask);

            var req = new CreateTopicRequest
            {
                Title = "Link",
                Type = "link",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicLinkRequest
                {
                    Description = "desc",
                    Url = "http://example.com"
                })
            };

            var res = await _svc.CreateTopicAsync(req);

            Assert.IsType<TopicLink>(res.Data);
        }

        [Fact]
        public async Task CreateTopicAsync_AssignmentType_Success()
        {
            SetupAddTopic();
            SetupCommit();

            _uow.Setup(x => x.TopicAssignments.AddAsync(It.IsAny<TopicAssignment>()))
                .Returns(Task.CompletedTask);

            var req = new CreateTopicRequest
            {
                Title = "Assign",
                Type = "assignment",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicAssignmentRequest
                {
                    Description = "desc",
                    MaximumFile = 3,
                    MaximumFileSize = "10"
                })
            };

            var res = await _svc.CreateTopicAsync(req);

            Assert.IsType<TopicAssignment>(res.Data);
        }

        [Fact]
        public async Task CreateTopicAsync_MeetingType_Success()
        {
            SetupAddTopic();
            SetupCommit();

            _uow.Setup(x => x.TopicMeetings.AddAsync(It.IsAny<TopicMeeting>()))
                .Returns(Task.CompletedTask);

            var req = new CreateTopicRequest
            {
                Title = "Meeting",
                Type = "meeting",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicMeetingRequest
                {
                    Open = DateTime.UtcNow,
                    Close = DateTime.UtcNow.AddDays(1)
                })
            };

            var res = await _svc.CreateTopicAsync(req);

            Assert.IsType<TopicMeeting>(res.Data);
        }

        [Fact]
        public async Task CreateTopicAsync_QuizType_Success()
        {
            SetupAddTopic();
            SetupCommit();

            _uow.Setup(x => x.TopicQuizzes.AddAsync(It.IsAny<TopicQuiz>()))
                .Returns(Task.CompletedTask);

            var req = new CreateTopicRequest
            {
                Title = "Quiz",
                Type = "quiz",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicQuizRequest
                {
                    Description = "quiz desc",
                    Questions = new List<TopicQuizQuestionRequest>
                    {
                        new()
                        {
                            QuestionName = "Q1",
                            QuestionText = "Text",
                            Type = "multiple",
                            DefaultMark = 1,
                            Choices = new List<TopicQuizQuestionChoiceRequest>
                            {
                                new() { Text="A", GradePercent=1 }
                            }
                        }
                    }
                })
            };

            var res = await _svc.CreateTopicAsync(req);

            Assert.IsType<TopicQuiz>(res.Data);
            Assert.Single(((TopicQuiz)res.Data).Questions);
        }

        #endregion

        #region ============================= UPDATE TOPIC TESTS =============================

        [Fact]
        public async Task UpdateTopicAsync_TopicNotFound_Throws()
        {
            var id = Guid.NewGuid();

            _uow.Setup(x => x.Topics.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Topic?)null);

            var req = new UpdateTopicRequest
            {
                Id = id
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.UpdateTopicAsync(req));
        }

        [Fact]
        public async Task UpdateTopicAsync_PageType_Success()
        {
            var topicId = Guid.NewGuid();

            _uow.Setup(x => x.Topics.GetByIdAsync(
                    topicId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Topic
                {
                    Id = topicId,
                    Title = "Old",
                    Type = "page",
                    SectionId = Guid.NewGuid()
                });


            _uow.Setup(x => x.TopicPages.FindAsync(
                    It.IsAny<Expression<Func<TopicPage, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TopicPage>
                {
                    new TopicPage
                    {
                        TopicId = topicId,
                        Description = "old",
                        Content = "old content"
                    }
                });


            SetupCommit();

            var req = new UpdateTopicRequest
            {
                Id = topicId,
                Type = "page",
                Title = "NewTitle",
                Data = JsonSerializer.Serialize(new UpdateTopicPageRequest
                {
                    Description = "updated",
                    Content = "content updated"
                })
            };

            var res = await _svc.UpdateTopicAsync(req);

            Assert.Equal("page", res.Type);
        }

        [Fact]
        public async Task UpdateTopicAsync_FileType_Success()
        {
            var topicId = Guid.NewGuid();

            _uow.Setup(x => x.Topics.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Topic { Id = topicId, Type = "file" });

            _uow.Setup(x => x.TopicFiles.FindAsync(
                    It.IsAny<Expression<Func<TopicFile, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TopicFile> { new TopicFile { TopicId = topicId } });

            SetupCommit();

            var req = new UpdateTopicRequest
            {
                Id = topicId,
                Type = "file",
                Data = JsonSerializer.Serialize(new UpdateTopicFileRequest { Description = "file updated" })
            };

            var res = await _svc.UpdateTopicAsync(req);

            Assert.Equal("file", res.Type);
        }

        [Fact]
        public async Task UpdateTopicAsync_LinkType_Success()
        {
            var topicId = Guid.NewGuid();


            _uow.Setup(x => x.Topics.GetByIdAsync(
                    topicId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Topic { Id = topicId, Type = "link" });

            _uow.Setup(x => x.TopicLinks.FindAsync(
                    It.IsAny<Expression<Func<TopicLink, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TopicLink> { new TopicLink { TopicId = topicId } });

            SetupCommit();

            var req = new UpdateTopicRequest
            {
                Id = topicId,
                Type = "link",
                Data = JsonSerializer.Serialize(new UpdateTopicLinkRequest
                {
                    Description = "d",
                    Url = "http://test"
                })
            };

            var res = await _svc.UpdateTopicAsync(req);

            Assert.Equal("link", res.Type);
        }

        [Fact]
        public async Task UpdateTopicAsync_AssignmentType_Success()
        {
            var topicId = Guid.NewGuid();

            var topic = new Topic
            {
                Id = topicId,
                Type = "assignment"
            };

            var assignment = new TopicAssignment
            {
                TopicId = topicId,
                Files = new List<CloudinaryFile>()
            };

            var topicAssignmentRepo = new Mock<ITopicAssignmentRepository>();
            var cloudinaryRepo = new Mock<IRepository<CloudinaryFile>>();

            _uow.Setup(x => x.Topics.GetByIdAsync(topicId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(topic);

            _uow.Setup(x => x.Topics.UpdateAsync(It.IsAny<Topic>()))
                .Returns(Task.CompletedTask);

            _uow.Setup(x => x.TopicAssignments)
                .Returns(topicAssignmentRepo.Object);

            topicAssignmentRepo.Setup(x =>
                    x.FindAsync(It.IsAny<Expression<Func<TopicAssignment, bool>>>(),
                                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TopicAssignment> { assignment });

            topicAssignmentRepo.Setup(x => x.UpdateAsync(It.IsAny<TopicAssignment>()))
                .Returns(Task.CompletedTask);

            _uow.Setup(x => x.CloudinaryFiles)
                .Returns(cloudinaryRepo.Object);

            cloudinaryRepo.Setup(x =>
                    x.FindAsync(It.IsAny<Expression<Func<CloudinaryFile, bool>>>(),
                                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CloudinaryFile>());

            cloudinaryRepo.Setup(x => x.AddAsync(It.IsAny<CloudinaryFile>()))
                .Returns(Task.CompletedTask);

            SetupCommit();

            var assignmentData = new
            {
                Description = "new",
                MaximumFile = 5,
                CloudinaryFiles = new[]
                {
                    new
                    {
                        Name = "file1.png",
                        DisplayUrl = "url",
                        DownloadUrl = "dl"
                    }
                }
            };

            var req = new UpdateTopicRequest
            {
                Id = topicId,
                Type = "assignment",
                Data = JsonSerializer.Serialize(assignmentData)
            };

            var res = await _svc.UpdateTopicAsync(req);

            Assert.Equal("assignment", res.Type);

            var updated = Assert.IsType<TopicAssignment>(res.Data);
            Assert.Equal("new", updated.Description);
            Assert.Single(updated.Files);
        }

        [Fact]
        public async Task UpdateTopicAsync_MeetingType_Success()
        {
            var topicId = Guid.NewGuid();

            _uow.Setup(x => x.Topics.GetByIdAsync(
                    topicId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Topic { Id = topicId, Type = "meeting" });

            _uow.Setup(x => x.TopicMeetings.FindAsync(
                    It.IsAny<Expression<Func<TopicMeeting, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TopicMeeting> { new TopicMeeting { TopicId = topicId } });

            SetupCommit();

            var req = new UpdateTopicRequest
            {
                Id = topicId,
                Type = "meeting",
                Data = JsonSerializer.Serialize(new UpdateTopicMeetingRequest
                {
                    Description = "new desc"
                })
            };

            var res = await _svc.UpdateTopicAsync(req);

            Assert.Equal("meeting", res.Type);
        }

        [Fact]
        public async Task UpdateTopicAsync_UnsupportedType_Throws()
        {
            var topicId = Guid.NewGuid();

            _uow.Setup(x => x.Topics.GetByIdAsync(
                    topicId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Topic { Id = topicId, Type = "weird" });

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                _svc.UpdateTopicAsync(new UpdateTopicRequest
                {
                    Id = topicId,
                    Type = "weird"
                }));
        }

        #endregion

        #region ============================= GET TOPIC BY ID TESTS =============================

        [Fact]
        public async Task GetTopicByIdAsync_NotFound_Throws()
        {
            _uow.Setup(x => x.Topics.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Topic?)null);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _svc.GetTopicByIdAsync(Guid.NewGuid()));

            Assert.Equal("Error fetching topic.", ex.Message);
        }

        [Fact]
        public async Task GetTopicByIdAsync_PageType_ReturnsPage()
        {
            var topicId = Guid.NewGuid();

            _uow.Setup(x => x.Topics.GetByIdAsync(
                    topicId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Topic { Id = topicId, Type = "page" });

            _uow.Setup(x => x.TopicPages.FindAsync(
                    It.IsAny<Expression<Func<TopicPage, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TopicPage> { new TopicPage { TopicId = topicId } });

            var res = await _svc.GetTopicByIdAsync(topicId);

            Assert.IsType<TopicPage>(res.Data);
        }


        #endregion

        #region ============================= DELETE TOPIC TESTS =============================

        [Fact]
        public async Task DeleteTopicAsync_NotFound_ReturnsFalse()
        {
            _uow.Setup(x => x.Topics.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Topic?)null);

            var result = await _svc.DeleteTopicAsync(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteTopicAsync_Success_ReturnsTrue()
        {
            var topic = new Topic { Id = Guid.NewGuid() };

            _uow.Setup(x => x.Topics.GetByIdAsync(
                    topic.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(topic);

            _uow.Setup(x => x.Topics.DeleteAsync(topic))
                .Returns(Task.CompletedTask);

            SetupCommit();

            var result = await _svc.DeleteTopicAsync(topic.Id);

            Assert.True(result);
        }

        #endregion
    }
}
