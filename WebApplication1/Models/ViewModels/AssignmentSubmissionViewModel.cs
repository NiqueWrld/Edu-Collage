using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class AssignmentSubmissionViewModel
    {
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public string ModuleName { get; set; }
        public string Comments { get; set; }
        public AssignmentSubmission PreviousSubmission { get; set; }

        public List<SubmissionViewModel> Submissions { get; set; }

        public string FileUrl { get; set; }
        public DateTime SubmissionDate { get; set; }

        public string? FeedbackFromLecturer { get; set; }
        public int? Grade { get; set; }
    }

    public class SubmissionViewModel
    {
        public IdentityUser Student { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int? Grade { get; set; }
        public string FileUrl { get; set; }
        public int SubmissionId { get; set; }
    }

}