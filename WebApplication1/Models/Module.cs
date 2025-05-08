using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Module
    {
        public int ModuleId { get; set; }

        [Required]
        public string ModuleName { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}
