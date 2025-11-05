using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class CreateCommentRequest
    {
        public Guid TopicId { get; set; }
        public string Text { get; set; } = null!;
    }
    public class GetCommentResponse
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = null!;
        public Guid UserId { get; set; }
        public Guid TopicId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
