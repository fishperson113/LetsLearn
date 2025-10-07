using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.Core.Entities;
using LetsLearn.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsLearn.UseCases.ServiceInterfaces;

namespace LetsLearn.UseCases.Services.MessageService
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MessageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateMessageAsync(CreateMessageRequest dto, Guid SenderId)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(dto.ConversationId);
            if (conversation == null)
            {
                throw new Exception("Conversation not found");
            }

            var userExists = await _unitOfWork.Users.ExistsAsync(u => u.Id == SenderId);
            if (!userExists)
            {
                throw new Exception("Sender not found");
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = dto.ConversationId,
                SenderId = SenderId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.Messages.AddAsync(message);
            await _unitOfWork.CommitAsync();

            conversation.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<GetMessageResponse>> GetMessagesByConversationIdAsync(Guid conversationId)
        {
            var messages = await _unitOfWork.Messages.GetMessagesByConversationIdAsync(conversationId);
            var dtos = new List<GetMessageResponse>();
            foreach (var msg in messages)
            {
                dtos.Add(new GetMessageResponse
                {
                    Id = msg.Id,
                    ConversationId = msg.ConversationId,
                    SenderId = msg.SenderId, 
                    Content = msg.Content,
                    Timestamp = msg.Timestamp
                });
            }
            return dtos;
        }

        public async Task<bool> IsUserInConversationAsync(Guid userId, Guid conversationId)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                return false;
            }

            return conversation.User1Id == userId || conversation.User2Id == userId;
        }
    }
}
