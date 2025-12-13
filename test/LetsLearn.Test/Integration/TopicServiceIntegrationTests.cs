using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.Infrastructure.Data;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using LetsLearn.UseCases.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace LetsLearn.Test.Integration
{
    public class TopicServiceIntegrationTests : IntegrationTestBase
    {
        protected override void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<ITopicService, TopicService>();
        }

        [Fact]
        public async Task CreateTopicAsync_RequestNull_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.CreateTopicAsync(null!));
        }

        [Fact]
        public async Task CreateTopicAsync_UnsupportedType_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            var req = new CreateTopicRequest
            {
                Title = "Invalid",
                Type = "invalid",
                SectionId = Guid.NewGuid()
            };

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.CreateTopicAsync(req));
        }

        [Fact]
        public async Task CreateTopicAsync_Page_Success()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var req = new CreateTopicRequest
            {
                Title = "Page 1",
                Type = "page",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicPageRequest
                {
                    Description = "desc",
                    Content = "content"
                })
            };

            var result = await service.CreateTopicAsync(req);

            Assert.Equal("page", result.Type);

            var page = (await uow.TopicPages.FindAsync(p => p.TopicId == result.Id))
                        .FirstOrDefault();

            Assert.NotNull(page);
            Assert.Equal("content", page.Content);
        }

        [Fact]
        public async Task CreateTopicAsync_Assignment_WithFiles_Success()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var req = new CreateTopicRequest
            {
                Title = "Assignment",
                Type = "assignment",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicAssignmentRequest
                {
                    Description = "A1",
                    MaximumFile = 2,
                    CloudinaryFiles = new List<TopicFileData>
                    {
                        new TopicFileData
                        {
                            Name = "file.pdf",
                            DownloadUrl = "url"
                        }
                    }
                })
            };

            var result = await service.CreateTopicAsync(req);

            var assignment = (await uow.TopicAssignments.FindAsync(a => a.TopicId == result.Id))
                              .FirstOrDefault();

            var files = await uow.CloudinaryFiles
                .FindAsync(f => f.TopicAssignmentId == result.Id);

            Assert.NotNull(assignment);
            Assert.Single(files);
        }

        [Fact]
        public async Task CreateTopicAsync_Quiz_Success()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            var req = new CreateTopicRequest
            {
                Title = "Quiz",
                Type = "quiz",
                SectionId = Guid.NewGuid(),
                Data = JsonSerializer.Serialize(new CreateTopicQuizRequest
                {
                    Description = "Quiz",
                    Questions = new List<TopicQuizQuestionRequest>()
                })
            };

            var result = await service.CreateTopicAsync(req);

            Assert.Equal("quiz", result.Type);
        }

        [Fact]
        public async Task UpdateTopicAsync_RequestNull_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.UpdateTopicAsync(null!));
        }

        [Fact]
        public async Task UpdateTopicAsync_TopicNotFound_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateTopicAsync(new UpdateTopicRequest
                {
                    Id = Guid.NewGuid(),
                    Type = "page"
                }));
        }

        [Fact]
        public async Task UpdateTopicAsync_Page_Success()
        {
            await using var scope = _provider.CreateAsyncScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();
            var ctx = scope.ServiceProvider.GetRequiredService<LetsLearnContext>();

            var topic = new Topic
            {
                Id = Guid.NewGuid(),
                Title = "Old",
                Type = "page",
                SectionId = Guid.NewGuid()
            };

            await uow.Topics.AddAsync(topic);
            await uow.TopicPages.AddAsync(new TopicPage
            {
                TopicId = topic.Id,
                Content = "old"
            });
            await uow.CommitAsync();

            ctx.ChangeTracker.Clear();

            var result = await service.UpdateTopicAsync(new UpdateTopicRequest
            {
                Id = topic.Id,
                Type = "page",
                Data = JsonSerializer.Serialize(new UpdateTopicPageRequest
                {
                    Content = "new"
                })
            });

            Assert.Equal("page", result.Type);

            var page = (await uow.TopicPages.FindAsync(p => p.TopicId == topic.Id)).First();
            Assert.Equal("new", page.Content);
        }

        [Fact]
        public async Task GetTopicByIdAsync_NotFound_Throws()
        {
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.GetTopicByIdAsync(Guid.NewGuid()));

            Assert.IsType<KeyNotFoundException>(ex.InnerException);
            Assert.Equal("Topic not found.", ex.InnerException!.Message);
        }

        [Fact]
        public async Task GetTopicByIdAsync_Assignment_LoadsFiles()
        {
            await using var scope = _provider.CreateAsyncScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            var topicId = Guid.NewGuid();

            await uow.Topics.AddAsync(new Topic
            {
                Id = topicId,
                Type = "assignment"
            });

            await uow.TopicAssignments.AddAsync(new TopicAssignment
            {
                TopicId = topicId,
                Files = new List<CloudinaryFile>
                {
                    new CloudinaryFile { Name = "a.pdf" }
                }
            });

            await uow.CommitAsync();

            var result = await service.GetTopicByIdAsync(topicId);

            var assignment = Assert.IsType<TopicAssignment>(result.Data);
            Assert.Single(assignment.Files);
        }

        [Fact]
        public async Task DeleteTopicAsync_Existing_ReturnsTrue()
        {
            await using var scope = _provider.CreateAsyncScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var service = scope.ServiceProvider.GetRequiredService<ITopicService>();

            var topic = new Topic
            {
                Id = Guid.NewGuid(),
                Type = "page"
            };

            await uow.Topics.AddAsync(topic);
            await uow.CommitAsync();

            var result = await service.DeleteTopicAsync(topic.Id);

            Assert.True(result);
        }
    }
}
