using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class CourseDTO
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Title cannot be empty")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public decimal? Price { get; set; }

        public string? Category { get; set; }

        public string? Level { get; set; }

        public bool IsPublished { get; set; } = false;

        public UserDTO? Creator { get; set; }

        public List<SectionDTO>? Sections { get; set; }

        public List<UserDTO>? Students { get; set; }
    }
}
