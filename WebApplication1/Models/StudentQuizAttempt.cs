using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class StudentQuizAttempt
    {
        [Key]
        public int AttemptId { get; set; }

        // Foreign key to Quiz
        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; }

        // Foreign key to Student (IdentityUser)
        public string StudentId { get; set; }
        [ForeignKey("StudentId")]
        public IdentityUser Student { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? SubmissionTime { get; set; }

        public bool IsSubmitted { get; set; } = false;

        public int? Score { get; set; }

        public string? FeedbackFromLecturer { get; set; }

        // Navigation property
        public virtual ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
    }
}