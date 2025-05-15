using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        ShortAnswer,
        Essay
    }

    public class QuizQuestion
    {
        [Key]
        public int QuestionId { get; set; }

        [Required]
        public string QuestionText { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public QuestionType Type { get; set; }

        public int Points { get; set; } = 1;

        // Foreign key to Quiz
        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; }

        // For serializing options (for multiple choice)
        public string? OptionsJson { get; set; }

        // For multiple choice or true/false questions
        public string? CorrectAnswer { get; set; }

        // Navigation property
        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
}