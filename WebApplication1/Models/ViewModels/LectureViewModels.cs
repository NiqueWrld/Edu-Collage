using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class LecturerDashboardViewModel
    {
        public List<ModuleStatViewModel> ModuleStats { get; set; } = new List<ModuleStatViewModel>();
    }

    public class ModuleStatViewModel
    {
        public Module Module { get; set; }
        public int StudyMaterialsCount { get; set; }
        public int QuizzesCount { get; set; }
        public int AssignmentsCount { get; set; }

    }

    public class LectureViewModels
    {
        public Module Module { get; set; }
        public int StudyMaterialsCount { get; set; }
        public int QuizzesCount { get; set; }
    }

    public class ModuleDetailsViewModel
    {
        public Module Module { get; set; }
        public List<AssignmentSubmission> Submissions { get; set; }
        public List<StudyMaterial> StudyMaterials { get; set; }
        public List<Assignment> Assignments { get; set; }
        public List<Quiz> Quizzes { get; set; }
    }

    public class AddStudyMaterialViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string CourseName { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public StudyMaterialType Type { get; set; }

        public string ResourceUrl { get; set; }

        public IFormFile ResourceFile { get; set; }
    }

    public class EditStudyMaterialViewModel
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string CourseName { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public StudyMaterialType Type { get; set; }

        public string ResourceUrl { get; set; }

        public IFormFile ResourceFile { get; set; }
    }

    public class CreateQuizViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string CourseName { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 180)]
        public int TimeLimit { get; set; } = 60;

        public int MaxAttempts { get; set; }

        public bool IsPublished { get; set; } = false;
    }

    public class EditQuizViewModel
    {
        public int QuizId { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string CourseName { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 180)]
        public int TimeLimit { get; set; } = 60;

        public int MaxAttempts { get; set; }

        public bool IsPublished { get; set; } = false;

        public List<QuizQuestion> Questions { get; set; }
    }

    public class AddQuizQuestionViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }

        [Required]
        public string QuestionText { get; set; }

        public QuestionType Type { get; set; }

        [Range(1, 100)]
        public int Points { get; set; } = 1;

        public List<string> Options { get; set; } = new List<string>();

        public string CorrectAnswer { get; set; }

        public IFormFile ImageFile { get; set; }
    }

    public class EditQuizQuestionViewModel
    {
        public int QuestionId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }

        [Required]
        public string QuestionText { get; set; }

        public QuestionType Type { get; set; }

        [Range(1, 100)]
        public int Points { get; set; } = 1;

        public List<string> Options { get; set; } = new List<string>();

        public string CorrectAnswer { get; set; }

        public IFormFile ImageFile { get; set; }

        public string ExistingImageUrl { get; set; }

        public bool RemoveImage { get; set; }
    }

    public class QuizAttemptsViewModel
    {
        public Quiz Quiz { get; set; }
        public List<StudentQuizAttempt> Attempts { get; set; }
    }

    public class GradeQuizAttemptViewModel
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public string StudentName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? SubmissionTime { get; set; }
        public string FeedbackFromLecturer { get; set; }
        public List<StudentAnswer> Answers { get; set; }
    }

    public class GradeQuizSubmissionViewModel
    {
        public int AttemptId { get; set; }
        public string FeedbackFromLecturer { get; set; }
        public List<GradedAnswerViewModel> GradedAnswers { get; set; }
    }

    public class GradedAnswerViewModel
    {
        public int AnswerId { get; set; }
        public int? PointsAwarded { get; set; }
        public string Feedback { get; set; }
        public bool? IsCorrect { get; set; }
    }

    public class StudentProgressViewModel
    {
        public Microsoft.AspNetCore.Identity.IdentityUser Student { get; set; }
        public int TotalQuizzes { get; set; }
        public int AttemptedQuizzes { get; set; }
        public int CompletedQuizzes { get; set; }
        public double AverageScore { get; set; }
        public List<StudentQuizAttempt> QuizAttempts { get; set; }
    }

    public class ModuleStudentProgressViewModel
    {
        public Module Module { get; set; }
        public List<StudentProgressViewModel> StudentProgressList { get; set; }
    }
}