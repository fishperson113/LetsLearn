using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class UserDTO
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [EmailAddress(ErrorMessage = "Email is not valid")]
        public string Email { get; set; } = null!;

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Username cannot be empty")]
        [MinLength(6, ErrorMessage = "Username must be at least 6 characters long")]
        public string Username { get; set; } = null!;

        public string? Role { get; set; }
        public string? Avatar { get; set; }

        public List<CourseDTO>? Courses { get; set; }
    }

    public class UpdateUserDTO
    {
        [MinLength(6, ErrorMessage = "Username must be at least 6 characters long")]
        public string? Username { get; set; }

        public string? Avatar { get; set; }
    }

    public class UpdatePasswordDTO
    {
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string OldPassword { get; set; } = null!;

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = null!;
    }
}
