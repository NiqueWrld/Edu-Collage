using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class StudentAnswer
    {
        [Key]
        public int AnswerId { get; set; }

        // Foreign key to QuizQuestion
        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public QuizQuestion Question { get; set; }

        // Foreign key to StudentQuizAttempt
        public int AttemptId { get; set; }
        [ForeignKey("AttemptId")]
        public StudentQuizAttempt Attempt { get; set; }

        [Required]
        public string Answer { get; set; }

        public bool? IsCorrect { get; set; }

        public int? PointsAwarded { get; set; }

        public string? FeedbackFromLecturer { get; set; }
    }
}