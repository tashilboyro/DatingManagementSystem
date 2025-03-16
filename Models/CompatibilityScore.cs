using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingManagementSystem.Models
{
    public class CompatibilityScore
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int User1Id { get; set; }  // First User

        [ForeignKey("User")]
        public int User2Id { get; set; }  // Second User

        public double Score { get; set; } // Compatibility Score
    }
}
