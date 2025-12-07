using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.QuestionSer;
using System.Linq.Expressions;
using System.Threading;

namespace LetsLearn.Test.Services
{
    public class QuestionServiceTests
    {
        [Fact]
        public async Task CreateAsync_NoChoices_SavesQuestionAndReturns()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            var qcRepo = new Mock<IQuestionChoiceRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            uow.Setup(x => x.QuestionChoices).Returns(qcRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var created = new Question { Id = Guid.NewGuid(), Choices = new List<QuestionChoice>() };
            qRepo.Setup(x => x.AddAsync(It.IsAny<Question>())).Returns(Task.CompletedTask);
            qRepo.Setup(x => x.GetWithChoicesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(created);

            var svc = new QuestionService(uow.Object);
            var req = new CreateQuestionRequest { QuestionName = "Q1", Choices = null };

            var resp = await svc.CreateAsync(req, Guid.NewGuid());
            Assert.Equal(created.Id, resp.Id);
        }

        [Fact]
        public async Task CreateAsync_WithChoices_AddsChoicesAndReturns()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            var qcRepo = new Mock<IQuestionChoiceRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            uow.Setup(x => x.QuestionChoices).Returns(qcRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var created = new Question { Id = Guid.NewGuid(), Choices = new List<QuestionChoice>() };
            qRepo.Setup(x => x.AddAsync(It.IsAny<Question>())).Returns(Task.CompletedTask);
            qcRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionChoice>>())).Returns(Task.CompletedTask);
            qRepo.Setup(x => x.GetWithChoicesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(created);

            var svc = new QuestionService(uow.Object);
            var req = new CreateQuestionRequest
            {
                QuestionName = "Q1",
                Choices = new List<CreateQuestionChoiceRequest> { new CreateQuestionChoiceRequest { Text = "A", GradePercent = 0 } }
            };

            var resp = await svc.CreateAsync(req, Guid.NewGuid());
            Assert.Equal(created.Id, resp.Id);
        }

        [Fact]
        public async Task CreateAsync_ReloadFails_ThrowsInvalidOperation()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            var qcRepo = new Mock<IQuestionChoiceRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            uow.Setup(x => x.QuestionChoices).Returns(qcRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            qRepo.Setup(x => x.AddAsync(It.IsAny<Question>())).Returns(Task.CompletedTask);
            qRepo.Setup(x => x.GetWithChoicesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Question)null!);

            var svc = new QuestionService(uow.Object);
            var req = new CreateQuestionRequest { QuestionName = "Q1" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(req, Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsKeyNotFound()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            qRepo.Setup(x => x.GetWithChoicesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Question)null!);

            var svc = new QuestionService(uow.Object);
            var req = new UpdateQuestionRequest { Id = Guid.NewGuid() };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateAsync(req, Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateAsync_UpdateFields_CorrectAnswerAndCourse()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            var q = new Question { Id = Guid.NewGuid(), Choices = new List<QuestionChoice>() };
            qRepo.Setup(x => x.GetWithChoicesAsync(q.Id, It.IsAny<CancellationToken>())).ReturnsAsync(q);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            qRepo.Setup(x => x.GetWithChoicesAsync(q.Id, It.IsAny<CancellationToken>())).ReturnsAsync(q);

            var svc = new QuestionService(uow.Object);
            var req = new UpdateQuestionRequest { Id = q.Id, CorrectAnswer = true, CourseId = "course-1" };
            var resp = await svc.UpdateAsync(req, Guid.NewGuid());

            Assert.True(resp.CorrectAnswer);
            Assert.Equal("course-1", resp.CourseId);
        }

        [Fact]
        public async Task UpdateAsync_ChoicesAddAndUpdate()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            var qcRepo = new Mock<IQuestionChoiceRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            uow.Setup(x => x.QuestionChoices).Returns(qcRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            var existingChoice = new QuestionChoice { Id = Guid.NewGuid(), Text = "old", GradePercent = 0 };
            var q = new Question { Id = Guid.NewGuid(), Choices = new List<QuestionChoice> { existingChoice } };
            qRepo.Setup(x => x.GetWithChoicesAsync(q.Id, It.IsAny<CancellationToken>())).ReturnsAsync(q);

            var svc = new QuestionService(uow.Object);
            var req = new UpdateQuestionRequest
            {
                Id = q.Id,
                Choices = new List<UpdateQuestionChoiceRequest>
                {
                    new UpdateQuestionChoiceRequest { Id = existingChoice.Id, Text = "new", GradePercent = 0 },
                    new UpdateQuestionChoiceRequest { Id = Guid.NewGuid(), Text = "added", GradePercent = 0 }
                }
            };
            var resp = await svc.UpdateAsync(req, Guid.NewGuid());

            Assert.NotNull(resp);
        }

        [Fact]
        public async Task UpdateAsync_ReloadUpdatedFails_ThrowsInvalidOperation()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            var q = new Question { Id = Guid.NewGuid(), Choices = new List<QuestionChoice>() };
            qRepo.SetupSequence(x => x.GetWithChoicesAsync(q.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(q)
                 .ReturnsAsync((Question)null!);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new QuestionService(uow.Object);
            var req = new UpdateQuestionRequest { Id = q.Id };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(req, Guid.NewGuid()));
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            qRepo.Setup(x => x.GetWithChoicesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Question)null!);

            var svc = new QuestionService(uow.Object);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetByIdAsync_Found_Returns()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            var q = new Question { Id = Guid.NewGuid() };
            qRepo.Setup(x => x.GetWithChoicesAsync(q.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(q);

            var svc = new QuestionService(uow.Object);
            var dto = await svc.GetByIdAsync(q.Id);

            Assert.Equal(q.Id, dto.Id);
        }

        [Fact]
        public async Task GetByCourseIdAsync_ReturnsList()
        {
            var uow = new Mock<IUnitOfWork>();
            var qRepo = new Mock<IQuestionRepository>();
            uow.Setup(x => x.Questions).Returns(qRepo.Object);
            qRepo.Setup(x => x.GetAllByCourseIdAsync("c1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Question> { new Question { Id = Guid.NewGuid() } });

            var svc = new QuestionService(uow.Object);
            var list = await svc.GetByCourseIdAsync("c1");
            Assert.Single(list);
        }
    }
}
