using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class CreateLecturerViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ManageLecturerModulesViewModel
    {
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public List<ModuleLecturer> AssignedModules { get; set; }
        public List<SelectListItem> AllModules { get; set; }
    }

    public class AssignModuleViewModel
    {
        public string LecturerId { get; set; }
        public int ModuleId { get; set; }
    }
}