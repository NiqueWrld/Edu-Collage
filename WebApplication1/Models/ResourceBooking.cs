using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public enum BookingStatus
    {
        Active,
        Returned,
        Overdue
    }

    public class ResourceBooking
    {
        [Key]
        public int BookingId { get; set; }

        public int ResourceId { get; set; }
        [ForeignKey("ResourceId")]
        public LibraryResource Resource { get; set; }

        [Required]
        public string IdentityUserId { get; set; }
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Active;

        [StringLength(4)]
        public string ReturnPin { get; set; }

        [StringLength(4)]
        public string CollectionPin { get; set; }

        // Automatic due date calculation based on resource type
        public void CalculateDueDate()
        {
            if (Resource.Type == ResourceType.Book)
            {
                // Books due in 7 days
                DueDate = BookingDate.AddDays(7);
            }
            else if (Resource.Type == ResourceType.Computer)
            {
                // Computers due in 1 hour
                DueDate = BookingDate.AddHours(1);
            }
        }
    }
}