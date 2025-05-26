using System.Reflection;

namespace WebApplication1.Models
{
    public class Assignment
    {
        public int AssignmentId { get; set; }
        public int ModuleId { get; set; }
        public Module Module { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string ResourceUrl { get; set; }
    }
}