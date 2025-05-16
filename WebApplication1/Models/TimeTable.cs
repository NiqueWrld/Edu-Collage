using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class ClassTime
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

    }
}