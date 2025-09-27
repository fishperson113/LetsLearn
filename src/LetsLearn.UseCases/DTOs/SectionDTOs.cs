using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class SectionDTO
    {
        public Guid Id { get; set; }

        public int Position { get; set; }

        [Required(ErrorMessage = "Title cannot be empty")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        // Nếu PK của Course bên chị là string, đổi Guid -> string cho phù hợp.
        public Guid CourseId { get; set; }

        public List<TopicDTO>? Topics { get; set; }
    }
}
