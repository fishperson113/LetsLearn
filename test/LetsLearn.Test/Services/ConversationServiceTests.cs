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
        // ------------------------------
        // 1. CreateConversationAsync
        // ------------------------------

        [Fact]
        public async Task CreateConversationAsync_UserMissing_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);

            // user1 = null ? user missing
            userRepo.SetupSequence(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((User)null!)  // user1 missing
                   .ReturnsAsync(new User());   // user2 exists (won’t reach here)

            var svc = new ConversationService(uow.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.CreateConversationAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateConversationAsync_ExistingConversation_ReturnsExisting()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            var convRepo = new Mock<IConversationRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);

            // Both users exist
            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User());

            // Existing conversation found
            convRepo.Setup(x => x.FindByUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Conversation { Id = Guid.NewGuid() });

            var svc = new ConversationService(uow.Object);

            var result = await svc.CreateConversationAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.NotEqual(Guid.Empty, result.Id);
        }

        [Fact]
        public async Task CreateConversationAsync_NewConversation_CreatesAndReturns()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            var convRepo = new Mock<IConversationRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);

            // Both users exist
            userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User());

            // No existing conversation
            convRepo.Setup(x => x.FindByUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Conversation)null!);

            // Creating new conversation
            convRepo.Setup(x => x.AddAsync(It.IsAny<Conversation>()))
                    .Returns(Task.CompletedTask);

            uow.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            var svc = new ConversationService(uow.Object);

            var result = await svc.CreateConversationAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.NotEqual(Guid.Empty, result.Id);
        }

        // ------------------------------------
        // 2. GetAllByUserIdAsync
        // ------------------------------------

        [Fact]
        public async Task GetAllByUserIdAsync_UserNotFound_Throws()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);

            // user does NOT exist
            userRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

            var svc = new ConversationService(uow.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.GetAllByUserIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_UserFound_ReturnsConversations()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IUserRepository>();
            var convRepo = new Mock<IConversationRepository>();

            uow.Setup(x => x.Users).Returns(userRepo.Object);
            uow.Setup(x => x.Conversations).Returns(convRepo.Object);

            // user exists
            userRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            // conversations found
            convRepo.Setup(x => x.FindAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Conversation>
                    {
                        new Conversation { Id = Guid.NewGuid() }
                    });

            var svc = new ConversationService(uow.Object);

            var list = await svc.GetAllByUserIdAsync(Guid.NewGuid());

            Assert.Single(list);
        }
    }
}
