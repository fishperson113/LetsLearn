using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class QuestionChoiceRequest
    {
        public Guid? Id { get; set; } //bo di
        public string? Content { get; set; }
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
    }

    public class QuestionRequest
    {
        public Guid? CreatedById { get; set; }
        public Guid? ModifiedById { get; set; }
        public String? CourseId { get; set; }
        public string? QuestionName { get; set; }
        public string? QuestionText { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public double? DefaultMark { get; set; }
        public long? Usage { get; set; }
        public string? FeedbackOfTrue { get; set; }
        public string? FeedbackOfFalse { get; set; }
        public bool? CorrectAnswer { get; set; }     
        public bool? Multiple { get; set; }        

        public List<QuestionChoiceRequest>? Choices { get; set; }
    }

    public class QuestionChoiceResponse
    {
        public Guid Id { get; set; }
        public Guid QuestionId { get; set; }
        public string? Content { get; set; }
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
    }

    public class QuestionResponse
    {
        public Guid? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public Guid? ModifiedById { get; set; }
        public string? ModifiedByName { get; set; }
        public String? CourseId { get; set; }
        public string? CourseName { get; set; }

        public Guid Id { get; set; }
        public string? QuestionName { get; set; }
        public string? QuestionText { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public double? DefaultMark { get; set; }
        public long? Usage { get; set; }
        public string? FeedbackOfTrue { get; set; }
        public string? FeedbackOfFalse { get; set; }
        public bool? CorrectAnswer { get; set; }     
        public bool? Multiple { get; set; }          

        public List<QuestionChoiceResponse>? Choices { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
