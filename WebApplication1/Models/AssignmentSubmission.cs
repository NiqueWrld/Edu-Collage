using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public string StudentId { get; set; }
        public string FileUrl { get; set; }
        public string? Comments { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? FeedbackFromLecturer { get; set; }
        public int? Grade { get; set; }

        public Assignment Assignment { get; set; }
        public IdentityUser Student { get; set; }
    }
}