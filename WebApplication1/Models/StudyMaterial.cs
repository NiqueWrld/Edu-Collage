using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public enum StudyMaterialType
    {
        Document,
        Video,
        Link,
        Assignment
    }

    public class StudyMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public StudyMaterialType Type { get; set; }

        // File path or URL
        public string ResourceUrl { get; set; }

        // Foreign key to Module
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; }

        // Foreign key to Lecturer (IdentityUser) who uploaded
        public string UploadedById { get; set; }
        [ForeignKey("UploadedById")]
        public IdentityUser UploadedBy { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}