using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class Quiz
    {
        [Key]
        public int QuizId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        // Foreign key to Module
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; }

        // Foreign key to Lecturer (IdentityUser) who created
        public string CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public IdentityUser CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int TimeLimit { get; set; }

        public int MaxAttempts { get; set; }

        public bool IsPublished { get; set; } = false;

        // Navigation property
        public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public virtual ICollection<StudentQuizAttempt> Attempts { get; set; } = new List<StudentQuizAttempt>();
    }
}