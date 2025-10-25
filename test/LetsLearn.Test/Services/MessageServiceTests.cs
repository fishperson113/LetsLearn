using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.MessageService;
using System.Threading;
using System.Linq.Expressions;

namespace LetsLearn.Test.Services
{
    public class MessageServiceTests
    {
        [Fact]
        public async Task CreateMessageAsync_ConversationNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var convRepo = new Mock<IConversationRepository>();
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);
            convRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Conversation)null!);

            var svc = new MessageService(uow.Object);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.CreateMessageAsync(new CreateMessageRequest { ConversationId = Guid.NewGuid(), Content = "hi" }, Guid.NewGuid()));
        }

        [Fact]
        public async Task IsUserInConversationAsync_NotFound_ReturnsFalse()
        {
            var uow = new Mock<IUnitOfWork>();
            var convRepo = new Mock<IConversationRepository>();
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);
            convRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Conversation)null!);
            var svc = new MessageService(uow.Object);
            var result = await svc.IsUserInConversationAsync(Guid.NewGuid(), Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task CreateMessageAsync_SenderNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var convRepo = new Mock<IConversationRepository>();
            var userRepo = new Mock<IUserRepository>();
            var msgRepo = new Mock<IMessageRepository>();
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Messages).Returns(msgRepo.Object);
            convRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Conversation { Id = Guid.NewGuid() });
            userRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

            var svc = new MessageService(uow.Object);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.CreateMessageAsync(new CreateMessageRequest { ConversationId = Guid.NewGuid(), Content = "hi" }, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateMessageAsync_Valid_SavesAndUpdates()
        {
            var uow = new Mock<IUnitOfWork>();
            var convRepo = new Mock<IConversationRepository>();
            var userRepo = new Mock<IUserRepository>();
            var msgRepo = new Mock<IMessageRepository>();
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);
            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Messages).Returns(msgRepo.Object);
            var conv = new Conversation { Id = Guid.NewGuid(), UpdatedAt = DateTime.UtcNow.AddDays(-1) };
            convRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(conv);
            userRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
            msgRepo.Setup(x => x.AddAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new MessageService(uow.Object);
            await svc.CreateMessageAsync(new CreateMessageRequest { ConversationId = conv.Id, Content = "hi" }, Guid.NewGuid());

            uow.Verify(x => x.CommitAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetMessagesByConversationIdAsync_ReturnsMapped()
        {
            var uow = new Mock<IUnitOfWork>();
            var msgRepo = new Mock<IMessageRepository>();
            uow.Setup(x => x.Messages).Returns(msgRepo.Object);
            msgRepo.Setup(x => x.GetMessagesByConversationIdAsync(It.IsAny<Guid>()))
                   .ReturnsAsync(new List<Message> { new Message { Id = Guid.NewGuid(), ConversationId = Guid.NewGuid() } });

            var svc = new MessageService(uow.Object);
            var list = await svc.GetMessagesByConversationIdAsync(Guid.NewGuid());
            Assert.Single(list);
        }

        [Fact]
        public async Task IsUserInConversationAsync_UserMatches_ReturnsTrue()
        {
            var uow = new Mock<IUnitOfWork>();
            var convRepo = new Mock<IConversationRepository>();
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);
            var userId = Guid.NewGuid();
            convRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Conversation { User1Id = userId, User2Id = Guid.NewGuid() });

            var svc = new MessageService(uow.Object);
            var result = await svc.IsUserInConversationAsync(userId, Guid.NewGuid());
            Assert.True(result);
        }

        [Fact]
        public async Task IsUserInConversationAsync_UserNotEither_ReturnsFalse()
        {
            var uow = new Mock<IUnitOfWork>();
            var convRepo = new Mock<IConversationRepository>();
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);
            convRepo.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Conversation { User1Id = Guid.NewGuid(), User2Id = Guid.NewGuid() });

            var svc = new MessageService(uow.Object);
            var result = await svc.IsUserInConversationAsync(Guid.NewGuid(), Guid.NewGuid());
            Assert.False(result);
        
        }
    }
}
