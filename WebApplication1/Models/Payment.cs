using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; } // Primary Key

        [Required]
        public decimal Amount { get; set; } // Payment Amount

        [Required]
        public DateTime PaymentDate { get; set; } // Date of Payment

        [Required]
        public string PaymentMethod { get; set; } // Payment Method (e.g., Credit Card, Bank Transfer)

        [Required]
        public string Status { get; set; } // Payment Status (e.g., Completed, Pending, Failed)

        // Foreign Key to Student
        [Required]
        public string IdentityUserId { get; set; }
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }
    }
}