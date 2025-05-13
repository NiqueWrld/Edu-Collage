using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class Application
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        // Status tracking dates (nullable)
        public DateTime? ReviewedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }

        public enum ApplicationStatus
        {
            Pending,
            UnderReview,
            Approved,
            Rejected
        }

        // Personal Information
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200)]
        public string Address { get; set; }

        // Course Information
        [Required(ErrorMessage = "Please select a course")]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        [Required(ErrorMessage = "Please select a study year")]
        [Display(Name = "Study Year")]
        public string StudyYear { get; set; }

        // Document Uploads
        [Display(Name = "Identification Document")]
        public string IdentificationDocumentPath { get; set; }

        [Display(Name = "Academic Records")]
        public string AcademicRecordsPath { get; set; }

        [Display(Name = "Motivation Letter")]
        public string? MotivationLetterPath { get; set; }

        // Application Fee
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Application Fee")]
        public decimal ApplicationFee { get; set; } = 50.00M;

        // Payment is nullable since it will only be required after approval
        public int? PaymentId { get; set; }

        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; }

        // User relationship
        public string IdentityUserId { get; set; }

        [ForeignKey("IdentityUserId")]
        public virtual IdentityUser IdentityUser { get; set; }

        // Admin notes on application (nullable)
        [StringLength(500)]
        public string? AdminComments { get; set; }

        // Application processing - Made nullable
        public string? ProcessedByUserId { get; set; }

        [ForeignKey("ProcessedByUserId")]
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual IdentityUser ProcessedByUser { get; set; }
    }
}