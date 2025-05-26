
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class GradeAssignmentViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string FileUrl { get; set; }
        public int? CurrentGrade { get; set; }

        [Required(ErrorMessage = "Please enter a grade")]
        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
        public int Grade { get; set; }

        [Display(Name = "Feedback")]
        public string Feedback { get; set; }
    }
}