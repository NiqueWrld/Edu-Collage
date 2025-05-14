using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public enum ResourceType
    {
        Book,
        Computer
    }

    public class LibraryResource
    {
        [Key]
        public int ResourceId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public ResourceType Type { get; set; }

        public string Description { get; set; }

        // Book specific properties
        public string? Author { get; set; }
        public string? ISBN { get; set; }

        // Computer specific properties
        public string? Specifications { get; set; }
        public string? Location { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        public virtual ICollection<ResourceBooking> Bookings { get; set; }
    }
}