using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.QuizResponseService;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LetsLearn.Test.Services
{
    public class QuizResponseServiceTests
    {
        // -----------------------------------------------------------
        // 1. CreateQuizResponseAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task CreateQuizResponseAsync_QuestionNotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var quizRepo = new Mock<IQuizResponseRepository>();
            var questionRepo = new Mock<IRepository<TopicQuizQuestion>>();

            uow.Setup(x => x.QuizResponses).Returns(quizRepo.Object);
            uow.Setup(x => x.TopicQuizQuestions).Returns(questionRepo.Object);

            questionRepo
                .Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TopicQuizQuestion)null!);

            var svc = new QuizResponseService(uow.Object);

            var dto = new QuizResponseRequest
            {
                TopicId = Guid.NewGuid(),
                Data = new QuizResponseData
                {
                    Answers = new List<QuizResponseAnswerDTO>
                    {
                        new QuizResponseAnswerDTO
                        {
                            TopicQuizQuestionId = Guid.NewGuid(),
                            Answer = "A",
                            Mark = 1
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<Exception>(() =>
                svc.CreateQuizResponseAsync(dto, Guid.NewGuid()));
        }



        [Fact]
        public async Task CreateQuizResponseAsync_ValidSingleAnswer_ReturnsDto()
        {
            var uow = new Mock<IUnitOfWork>();
            var quizRepo = new Mock<IQuizResponseRepository>();
            var questionRepo = new Mock<IRepository<TopicQuizQuestion>>();
            var questionBankRepo = new Mock<IQuestionRepository>();

            uow.Setup(x => x.QuizResponses).Returns(quizRepo.Object);
            uow.Setup(x => x.TopicQuizQuestions).Returns(questionRepo.Object);
            uow.Setup(x => x.Questions).Returns(questionBankRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var question = new TopicQuizQuestion
            {
                Id = Guid.NewGuid(),
                QuestionName = "Q1",
                QuestionText = "Text",
                Type = "MCQ",
                DefaultMark = 1,
                Choices = new List<TopicQuizQuestionChoice>()
            };

            questionRepo.Setup(x => x.GetByIdAsync(question.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(question);

            quizRepo.Setup(x => x.AddAsync(It.IsAny<QuizResponse>()))
                    .Returns(Task.CompletedTask);

            questionBankRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Question>());

            var svc = new QuizResponseService(uow.Object);

            var dto = new QuizResponseRequest
            {
                TopicId = Guid.NewGuid(),
                Data = new QuizResponseData
                {
                    Answers = new List<QuizResponseAnswerDTO>
                    {
                        new QuizResponseAnswerDTO
                        {
                            TopicQuizQuestionId = question.Id,
                            Answer = "ABC",
                            Mark = 1
                        }
                    }
                }
            };

            var result = await svc.CreateQuizResponseAsync(dto, Guid.NewGuid());

            Assert.Equal(dto.TopicId, result.TopicId);
            Assert.Single(result.Data.Answers);
            Assert.Equal("ABC", result.Data.Answers[0].Answer);
        }

        // -----------------------------------------------------------
        // 2. UpdateQuizResponseByIdAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task UpdateQuizResponseAsync_NotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var quizRepo = new Mock<IQuizResponseRepository>();

            uow.Setup(x => x.QuizResponses).Returns(quizRepo.Object);

            quizRepo.Setup(x => x.GetByIdTrackedWithAnswersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((QuizResponse)null!);

            var svc = new QuizResponseService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.UpdateQuizResponseByIdAsync(Guid.NewGuid(), new QuizResponseRequest()));
        }


        [Fact]
        public async Task UpdateQuizResponseAsync_QuestionNotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var quizRepo = new Mock<IQuizResponseRepository>();
            var answerRepo = new Mock<IQuizResponseAnswerRepository>();
            var questionRepo = new Mock<IRepository<TopicQuizQuestion>>();

            var existing = new QuizResponse
            {
                Id = Guid.NewGuid(),
                Answers = new List<QuizResponseAnswer>()
            };

            uow.Setup(x => x.QuizResponses).Returns(quizRepo.Object);
            uow.Setup(x => x.QuizResponseAnswers).Returns(answerRepo.Object);
            uow.Setup(x => x.TopicQuizQuestions).Returns(questionRepo.Object);

            quizRepo.Setup(x => x.GetByIdTrackedWithAnswersAsync(existing.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existing);

            questionRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TopicQuizQuestion)null!);

            var svc = new QuizResponseService(uow.Object);

            var dto = new QuizResponseRequest
            {
                Data = new QuizResponseData
                {
                    Answers = new List<QuizResponseAnswerDTO>
                    {
                        new QuizResponseAnswerDTO
                        {
                            TopicQuizQuestionId = Guid.NewGuid(),
                            Answer = "XYZ",
                            Mark = 2
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<Exception>(() =>
                svc.UpdateQuizResponseByIdAsync(existing.Id, dto));
        }



        [Fact]
        public async Task UpdateQuizResponseAsync_Valid_ReturnsUpdatedDto()
        {
            var uow = new Mock<IUnitOfWork>();
            var quizRepo = new Mock<IQuizResponseRepository>();
            var answerRepo = new Mock<IQuizResponseAnswerRepository>();
            var questionRepo = new Mock<IRepository<TopicQuizQuestion>>();

            var existing = new QuizResponse
            {
                Id = Guid.NewGuid(),
                Answers = new List<QuizResponseAnswer>
                {
                    new QuizResponseAnswer { Id = Guid.NewGuid() }
                }
            };

            var question = new TopicQuizQuestion
            {
                Id = Guid.NewGuid(),
                QuestionName = "Q1",
                QuestionText = "TXT",
                Type = "MCQ",
                DefaultMark = 1,
                Choices = new List<TopicQuizQuestionChoice>()
            };

            uow.Setup(x => x.QuizResponses).Returns(quizRepo.Object);
            uow.Setup(x => x.QuizResponseAnswers).Returns(answerRepo.Object);
            uow.Setup(x => x.TopicQuizQuestions).Returns(questionRepo.Object);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            quizRepo.Setup(x => x.GetByIdTrackedWithAnswersAsync(existing.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existing);

            answerRepo.Setup(x => x.DeleteRangeAsync(existing.Answers))
                      .Returns(Task.CompletedTask);

            questionRepo.Setup(x => x.GetByIdAsync(question.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(question);

            var svc = new QuizResponseService(uow.Object);

            var dto = new QuizResponseRequest
            {
                Data = new QuizResponseData
                {
                    Answers = new List<QuizResponseAnswerDTO>
                    {
                        new QuizResponseAnswerDTO
                        {
                            TopicQuizQuestionId = question.Id,
                            Answer = "NEWANSWER",
                            Mark = 3
                        }
                    }
                }
            };

            var result = await svc.UpdateQuizResponseByIdAsync(existing.Id, dto);

            Assert.Single(result.Data.Answers);
            Assert.Equal("NEWANSWER", result.Data.Answers[0].Answer);
        }



        // -----------------------------------------------------------
        // 3. GetQuizResponseByIdAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task GetQuizResponseByIdAsync_NotFound_ThrowsException()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IQuizResponseRepository>();

            uow.Setup(x => x.QuizResponses).Returns(repo.Object);
            repo.Setup(x => x.GetByIdTrackedWithAnswersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuizResponse)null!);

            var svc = new QuizResponseService(uow.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                svc.GetQuizResponseByIdAsync(Guid.NewGuid()));
        }


        [Fact]
        public async Task GetQuizResponseByIdAsync_Valid_ReturnsDto()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IQuizResponseRepository>();

            var entity = new QuizResponse
            {
                Id = Guid.NewGuid(),
                Answers = new List<QuizResponseAnswer>()
            };

            uow.Setup(x => x.QuizResponses).Returns(repo.Object);
            repo.Setup(x => x.GetByIdTrackedWithAnswersAsync(entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var svc = new QuizResponseService(uow.Object);

            var result = await svc.GetQuizResponseByIdAsync(entity.Id);

            Assert.Equal(entity.Id, result.Id);
        }



        // -----------------------------------------------------------
        // 4. GetAllQuizResponsesByTopicIdAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task GetAllQuizResponsesByTopicIdAsync_ReturnsList()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IQuizResponseRepository>();

            uow.Setup(x => x.QuizResponses).Returns(repo.Object);

            repo.Setup(x => x.FindAllByTopicIdWithAnswersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuizResponse>
                {
                    new QuizResponse { Id = Guid.NewGuid(), Answers = new List<QuizResponseAnswer>() }
                });

            var svc = new QuizResponseService(uow.Object);

            var result = await svc.GetAllQuizResponsesByTopicIdAsync(Guid.NewGuid());

            Assert.Single(result);
        }



        // -----------------------------------------------------------
        // 5. GetAllQuizResponsesByTopicIdOfStudentAsync
        // -----------------------------------------------------------

        [Fact]
        public async Task GetAllQuizResponsesByTopicIdOfStudentAsync_ReturnsList()
        {
            var uow = new Mock<IUnitOfWork>();
            var repo = new Mock<IQuizResponseRepository>();

            uow.Setup(x => x.QuizResponses).Returns(repo.Object);

            repo.Setup(x =>
                    x.FindByTopicIdAndStudentIdWithAnswersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuizResponse>
                {
                    new QuizResponse { Id = Guid.NewGuid(), Answers = new List<QuizResponseAnswer>() }
                });

            var svc = new QuizResponseService(uow.Object);

            var result = await svc.GetAllQuizResponsesByTopicIdOfStudentAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.Single(result);
        }
    }
}
