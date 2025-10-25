using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LetsLearn.Core.Interfaces;
using LetsLearn.Core.Entities;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.Services.ConversationService;
using System.Threading;
using System.Linq.Expressions;

namespace LetsLearn.Test.Services
{
    public class ConversationServiceTests
    {
        [Fact]
        public async Task GetOrCreateConversationAsync_UserMissing_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            uow.Setup(x => x.Users).Returns(users.Object);
            users.SetupSequence(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User)null!)
                 .ReturnsAsync(new User());

            var svc = new ConversationService(uow.Object);
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetOrCreateConversationAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_Existing_Returns()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var conv = new Mock<IConversationRepository>();
            uow.Setup(x => x.Users).Returns(users.Object);
            uow.Setup(x => x.Conversations).Returns(conv.Object);
            users.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new User());
            conv.Setup(x => x.FindByUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Conversation { Id = Guid.NewGuid() });

            var svc = new ConversationService(uow.Object);
            var dto = await svc.GetOrCreateConversationAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.NotEqual(Guid.Empty, dto.Id);
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_New_CreatesAndReturns()
        {
            var uow = new Mock<IUnitOfWork>();
            var users = new Mock<IUserRepository>();
            var conv = new Mock<IConversationRepository>();
            uow.Setup(x => x.Users).Returns(users.Object);
            uow.Setup(x => x.Conversations).Returns(conv.Object);
            users.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new User());
            conv.Setup(x => x.FindByUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Conversation)null!);
            conv.Setup(x => x.AddAsync(It.IsAny<Conversation>())).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new ConversationService(uow.Object);
            var dto = await svc.GetOrCreateConversationAsync(Guid.NewGuid(), Guid.NewGuid());
            Assert.NotEqual(Guid.Empty, dto.Id);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_UserNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var conv = new Mock<IConversationRepository>();
            uow.Setup(x => x.Conversations).Returns(conv.Object);
            conv.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Conversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var svc = new ConversationService(uow.Object);
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetAllByUserIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_UserFound_Returns()
        {
            var uow = new Mock<IUnitOfWork>();
            var conv = new Mock<IConversationRepository>();
            uow.Setup(x => x.Conversations).Returns(conv.Object);
            conv.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Conversation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            conv.Setup(x => x.FindAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Conversation> { new Conversation { Id = Guid.NewGuid() } });

            var svc = new ConversationService(uow.Object);
            var list = await svc.GetAllByUserIdAsync(Guid.NewGuid());
            Assert.Single(list);
        }
    }
}
