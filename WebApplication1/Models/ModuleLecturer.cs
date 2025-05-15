using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class ModuleLecturer
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to Module
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; }

        // Foreign key to Lecturer (IdentityUser)
        public string LecturerId { get; set; }
        [ForeignKey("LecturerId")]
        public IdentityUser Lecturer { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}