using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class LibraryBooking
    {
        [Key]
        public int BookingId { get; set; } // Primary Key

        [Required]
        public string ResourceName { get; set; } // Name of the resource (e.g., Book Title, Room Number)

        [Required]
        public DateTime BookingDate { get; set; } // Date of the Booking

        [Required]
        public DateTime StartTime { get; set; } // Booking Start Time

        [Required]
        public DateTime EndTime { get; set; } // Booking End Time

        [Required]
        public string Status { get; set; } // Status of the Booking (e.g., Confirmed, Cancelled)

        // Foreign Key to IdentityUser
        [Required]
        public string IdentityUserId { get; set; }
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }
    }
}