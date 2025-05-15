using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLecturerController : Controller
    {
        private readonly NexelContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminLecturerController(
            NexelContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: All lecturers
        public async Task<IActionResult> Index()
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            return View(lecturers);
        }

        // GET: Create new lecturer
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create new lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateLecturerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Ensure Lecturer role exists
                    if (!await _roleManager.RoleExistsAsync("Lecturer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Lecturer"));
                    }

                    // Assign the Lecturer role
                    await _userManager.AddToRoleAsync(user, "Lecturer");

                    TempData["SuccessMessage"] = "Lecturer account created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Manage module assignments
        public async Task<IActionResult> ManageModules(string id)
        {
            var lecturer = await _userManager.FindByIdAsync(id);

            if (lecturer == null)
            {
                return NotFound();
            }

            // Get modules already assigned to this lecturer
            var assignedModules = await _context.ModuleLecturers
                .Where(ml => ml.LecturerId == id)
                .Include(ml => ml.Module)
                .ThenInclude(m => m.Course)
                .ToListAsync();

            // Get all modules 
            var allModules = await _context.Modules
                .Include(m => m.Course)
                .OrderBy(m => m.Course.Faculty)
                .ThenBy(m => m.Course.CourseName)
                .ThenBy(m => m.ModuleName)
                .ToListAsync();

            var viewModel = new ManageLecturerModulesViewModel
            {
                LecturerId = id,
                LecturerName = lecturer.UserName,
                AssignedModules = assignedModules,
                AllModules = allModules
                    .Select(m => new SelectListItem
                    {
                        Value = m.ModuleId.ToString(),
                        Text = $"{m.Course.Faculty} - {m.Course.CourseName} - {m.ModuleName} ({m.ModuleCode})"
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: Assign module to lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignModule(AssignModuleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if assignment already exists
                var existingAssignment = await _context.ModuleLecturers
                    .FirstOrDefaultAsync(ml =>
                        ml.ModuleId == model.ModuleId &&
                        ml.LecturerId == model.LecturerId);

                if (existingAssignment == null)
                {
                    var assignment = new ModuleLecturer
                    {
                        ModuleId = model.ModuleId,
                        LecturerId = model.LecturerId,
                        AssignedDate = DateTime.UtcNow
                    };

                    _context.ModuleLecturers.Add(assignment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Module assigned successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "This module is already assigned to the lecturer.";
                }
            }

            return RedirectToAction(nameof(ManageModules), new { id = model.LecturerId });
        }

        // POST: Unassign module from lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignModule(int assignmentId, string lecturerId)
        {
            var assignment = await _context.ModuleLecturers.FindAsync(assignmentId);

            if (assignment == null)
            {
                return NotFound();
            }

            _context.ModuleLecturers.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Module unassigned successfully.";
            return RedirectToAction(nameof(ManageModules), new { id = lecturerId });
        }
    }
}