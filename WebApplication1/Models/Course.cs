using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        public string Faculty { get; set; }

        [Required]
        public string CourseName { get; set; }

        [Required]
        public string CourseCode { get; set; }

        public string Description { get; set; }

        [Range(1, 6)]
        public int DurationYears { get; set; }

        // Navigation property for related Modules
        public ICollection<Module> Modules { get; set; } = new List<Module>();
    }
}
