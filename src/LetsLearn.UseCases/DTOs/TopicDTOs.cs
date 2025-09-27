using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class TopicDTO
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid SectionId { get; set; }

        [Required(ErrorMessage = "Title cannot be empty")]
        public string Title { get; set; } = null!;

        public string? Type { get; set; }

        public string? Data { get; set; }

        public int? StudentCount { get; set; }

        public string? Response { get; set; }

        public CourseDTO? Course { get; set; }
    }
}
