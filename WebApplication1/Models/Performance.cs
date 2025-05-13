using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Performance
    {
        [Key]
        public int PerformanceId { get; set; } // Primary Key

        [Required]
        public string CourseName { get; set; } // Name of the Course

        [Required]
        public decimal Grade { get; set; } // Grade Achieved

        public string Remarks { get; set; } // Additional Remarks (e.g., "Excellent", "Needs Improvement")

        // Foreign Key to Student
        [Required]
        public string IdentityUserId { get; set; }
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }
    }
}