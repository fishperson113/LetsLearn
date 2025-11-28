using LetsLearn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class QuizResponseRequest
    {
        public Guid TopicId { get; set; }
        public QuizResponseData Data { get; set; } = new();
    }

    public class QuizResponseData
    {
        public string? Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<QuizResponseAnswerDTO> Answers { get; set; } = new();
    }

    public class QuizResponseAnswerDTO
    {
        public Guid TopicQuizQuestionId { get; set; }
        public string? Answer { get; set; } = "";
        public decimal? Mark { get; set; }
    }

    public class QuizResponseDTO
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid TopicId { get; set; }

        public QuizResponseData Data { get; set; } = new();
    }
}