using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.CommentService
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CommentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddCommentAsync(Guid commenterId, CreateCommentRequest dto, CancellationToken ct = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(commenterId, ct)
                ?? throw new Exception("Người dùng không tồn tại");


            //Check if Topic exists
            var topic = await _unitOfWork.Topics.GetByIdAsync(dto.TopicId, ct)
                ?? throw new Exception("Chủ đề không tồn tại");

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                UserId = commenterId,
                TopicId = dto.TopicId,
                Text = dto.Text,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<GetCommentResponse>> GetCommentsByTopicAsync(Guid topicId, CancellationToken ct = default)
        {
            var comments = await _unitOfWork.Comments.FindByTopicIdAsync(topicId, ct);

            return comments.Select(c => new GetCommentResponse
            {
                Id = c.Id,
                Text = c.Text,
                UserId = c.UserId,
                TopicId = c.TopicId,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default)
        {
            var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == commentId, ct)
                ?? throw new Exception("Bình luận không tồn tại");

            await _unitOfWork.Comments.DeleteAsync(comment);
            await _unitOfWork.CommitAsync();
        }
    }
}
