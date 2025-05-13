using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class Student
    {
        public int StudentId { get; set; } // Primary Key

        // Foreign Key to IdentityUser
        public string IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; }

        // Additional Student-Specific Properties
        public DateTime DateOfBirth { get; set; }

        // Navigation Properties
        public ICollection<Application> Applications { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<LibraryBooking> LibraryBookings { get; set; }
        public ICollection<Performance> Performances { get; set; }
    }
}
