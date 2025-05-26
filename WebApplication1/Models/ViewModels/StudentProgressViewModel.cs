using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class StudentProgressViewModel2
    {
        public Module Module { get; set; }
        public List<StudentQuizProgressViewModel> QuizProgress { get; set; }
    }

    public class StudentQuizProgressViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int? AttemptId { get; set; }
        public DateTime? AttemptDate { get; set; }
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public decimal Percentage { get; set; }
        public string Status { get; set; }
    }
}