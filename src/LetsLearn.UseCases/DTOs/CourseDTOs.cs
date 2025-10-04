using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class CourseRequest
    {
        public String Id { get; set; }
        public Guid CreatorId { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; }
        public decimal? Price { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class CourseResponse
    {
        public String Id { get; set; }
        public Guid CreatorId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int TotalJoined { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; }
        public bool? IsPublished { get; set; }

        public List<SectionResponse>? Sections { get; set; }
    }
}
