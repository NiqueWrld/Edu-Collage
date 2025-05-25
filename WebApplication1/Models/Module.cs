using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Module
    {
        public int ModuleId { get; set; }

        [Required]
        public string ModuleName { get; set; }


        [Required]
        public string ModuleCode { get; set; }

        [Required]
        public decimal ModulePrice { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Year { get; set; }

        public DayOfWeek? ClassDay { get; set; }
        public TimeSpan? ClassTime { get; set; }

        public SemesterNumber Semester { get; set; }

        public enum SemesterNumber
        {
            Semester1,
            Semester2,
        }

        // Foreign key to Course
        public int CourseId { get; set; }

        // Navigation property
        public Course Course { get; set; }
    }
}
