using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;

namespace WebApplication1.Models
{
    public class Application
    {
        [Key]
        public int ApplicationId { get; set; } 

        [Required]
        public DateTime ApplicationDate { get; set; } 

        [Required]
        public ApplicationStatus Status { get; set; } 

        public enum ApplicationStatus
        {
            Pending,
            Approved,
            Rejected
        }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } 


        [Required]
        public string CourseId { get; set; } 
        public Course Course { get; set; }

        [Required]
        public string StudyYear { get; set; }


        [Required]
        public decimal ApplicationFee { get; set; }

        public int PaymentId { get; set; } 
        public Payment Payment { get; set; }

        [Required]
        public string IdentityUserId { get; set; }
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }
    }
}