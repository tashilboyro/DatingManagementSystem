using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // Needed for IFormFile

namespace DatingManagementSystem.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(100)]
        public required string FirstName { get; set; }

        [Required, MaxLength(100)]
        public required string LastName { get; set; }

        [Required]
        public int Age { get; set; }

        [Required, MaxLength(10)]
        public required string Gender { get; set; }

        [Required, EmailAddress, MaxLength(255)]
        public required string Email { get; set; }

        [Required, MaxLength(255)]
        public required string Password { get; set; } // Store hashed password

        public string Interests { get; set; } = string.Empty; // Default empty string

        public byte[]? ProfilePicture { get; set; } // Allow nullable profile picture

        [NotMapped]
        public IFormFile? ProfilePictureFile { get; set; } // Single file, not an array

        [MaxLength(500)]
        public required string Bio { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}