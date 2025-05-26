using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class CreateAssignmentViewModel
    {
        [Required]
        public Assignment Assignment { get; set; } = new Assignment();
        public IFormFile? ResourceFile { get; set; }
    }
}