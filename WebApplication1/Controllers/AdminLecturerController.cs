using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLecturerController : Controller
    {
        private readonly NexelContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly NotificationService _notificationService;
        TimeZoneInfo southAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

        public AdminLecturerController(
            NexelContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _notificationService = notificationService;
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

                    // Notify admins about new lecturer
                    var admins = await _userManager.GetUsersInRoleAsync("Admin");
                    var currentAdmin = await _userManager.GetUserAsync(User);
                    var otherAdminIds = admins
                        .Where(a => a.Id != currentAdmin.Id)
                        .Select(a => a.Id)
                        .ToList();
                    
                    if (otherAdminIds.Any())
                    {
                        await _notificationService.CreateBulkNotificationsAsync(
                            otherAdminIds,
                            "New Lecturer Account Created",
                            $"A new lecturer account has been created: {model.Email}",
                            "/AdminLecturer/Index",
                            NotificationType.System
                        );
                    }
                    
                    // Send notification to the new lecturer
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "Welcome to Nexel",
                        "Your lecturer account has been created. You can now log in and manage your modules.",
                        "/Lecturer/Dashboard",
                        NotificationType.System
                    );

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
                        AssignedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone)
                    };

                    _context.ModuleLecturers.Add(assignment);
                    await _context.SaveChangesAsync();

                    // Get module and lecturer details for notification
                    var module = await _context.Modules
                        .Include(m => m.Course)
                        .FirstOrDefaultAsync(m => m.ModuleId == model.ModuleId);

                    // Notify the lecturer about the new module assignment
                    await _notificationService.CreateNotificationAsync(
                        model.LecturerId,
                        "New Module Assignment",
                        $"You have been assigned to teach {module.ModuleName} ({module.ModuleCode}) in {module.Course.CourseName}.",
                        $"/Lecturer/ModuleDetails/{module.ModuleId}",
                        NotificationType.System
                    );

                    // Get students enrolled in this course to notify them
                    var studentsInCourse = await _context.Applications
                        .Where(a => a.CourseId == module.CourseId && 
                                  a.Status == Application.ApplicationStatus.Approved && 
                                  a.PaymentId != null)
                        .Select(a => a.IdentityUserId)
                        .ToListAsync();

                    if (studentsInCourse.Any())
                    {
                        var lecturer = await _userManager.FindByIdAsync(model.LecturerId);
                        
                        await _notificationService.CreateBulkNotificationsAsync(
                            studentsInCourse,
                            "New Lecturer Assigned",
                            $"A new lecturer ({lecturer.Email}) has been assigned to your module: {module.ModuleName}.",
                            $"/Student/ModuleDetails/{module.ModuleId}",
                            NotificationType.System
                        );
                    }

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
            var assignment = await _context.ModuleLecturers
                .Include(ml => ml.Module)
                .ThenInclude(m => m.Course)
                .Include(ml => ml.Lecturer)
                .FirstOrDefaultAsync(ml => ml.Id == assignmentId);

            if (assignment == null)
            {
                return NotFound();
            }

            // Store module details before removing the assignment
            var moduleId = assignment.ModuleId;
            var moduleName = assignment.Module.ModuleName;
            var moduleCode = assignment.Module.ModuleCode;
            var courseName = assignment.Module.Course.CourseName;
            var lecturerEmail = assignment.Lecturer.Email;

            _context.ModuleLecturers.Remove(assignment);
            await _context.SaveChangesAsync();

            // Notify the lecturer about being unassigned
            await _notificationService.CreateNotificationAsync(
                lecturerId,
                "Module Assignment Removed",
                $"You have been unassigned from teaching {moduleName} ({moduleCode}) in {courseName}.",
                "/Lecturer/Dashboard",
                NotificationType.System
            );

            // Get students enrolled in this course to notify them
            var studentsInCourse = await _context.Applications
                .Where(a => a.CourseId == assignment.Module.CourseId && 
                          a.Status == Application.ApplicationStatus.Approved && 
                          a.PaymentId != null)
                .Select(a => a.IdentityUserId)
                .ToListAsync();

            if (studentsInCourse.Any())
            {
                await _notificationService.CreateBulkNotificationsAsync(
                    studentsInCourse,
                    "Lecturer Assignment Changed",
                    $"Lecturer {lecturerEmail} has been unassigned from your module: {moduleName}. A new lecturer will be assigned soon.",
                    $"/Student/ModuleDetails/{moduleId}",
                    NotificationType.System
                );
            }

            TempData["SuccessMessage"] = "Module unassigned successfully.";
            return RedirectToAction(nameof(ManageModules), new { id = lecturerId });
        }
        
        // POST: Deactivate lecturer account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateLecturer(string id)
        {
            var lecturer = await _userManager.FindByIdAsync(id);
            
            if (lecturer == null)
            {
                return NotFound();
            }
            
            // Get modules assigned to this lecturer
            var assignedModules = await _context.ModuleLecturers
                .Where(ml => ml.LecturerId == id)
                .Include(ml => ml.Module)
                .ThenInclude(m => m.Course)
                .ToListAsync();
            
            // Store module information before removing assignments
            var moduleDetails = assignedModules.Select(am => new {
                ModuleId = am.ModuleId,
                ModuleName = am.Module.ModuleName,
                CourseId = am.Module.CourseId
            }).ToList();
            
            // Remove all module assignments
            _context.ModuleLecturers.RemoveRange(assignedModules);
            await _context.SaveChangesAsync();
            
            // Deactivate the account (lock it instead of deleting)
            await _userManager.SetLockoutEndDateAsync(lecturer, DateTimeOffset.MaxValue);
            await _userManager.UpdateSecurityStampAsync(lecturer);
            
            // Notify other admins
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var currentAdmin = await _userManager.GetUserAsync(User);
            var otherAdminIds = admins
                .Where(a => a.Id != currentAdmin.Id)
                .Select(a => a.Id)
                .ToList();
            
            if (otherAdminIds.Any())
            {
                await _notificationService.CreateBulkNotificationsAsync(
                    otherAdminIds,
                    "Lecturer Account Deactivated",
                    $"Lecturer account {lecturer.Email} has been deactivated and all module assignments have been removed.",
                    "/AdminLecturer/Index",
                    NotificationType.System
                );
            }
            
            // Notify students for each module that was assigned to this lecturer
            foreach (var module in moduleDetails)
            {
                var studentsInCourse = await _context.Applications
                    .Where(a => a.CourseId == module.CourseId && 
                           a.Status == Application.ApplicationStatus.Approved && 
                           a.PaymentId != null)
                    .Select(a => a.IdentityUserId)
                    .ToListAsync();
                
                if (studentsInCourse.Any())
                {
                    await _notificationService.CreateBulkNotificationsAsync(
                        studentsInCourse,
                        "Module Lecturer Change",
                        $"The lecturer for {module.ModuleName} has changed. A new lecturer will be assigned soon.",
                        $"/Student/ModuleDetails/{module.ModuleId}",
                        NotificationType.System
                    );
                }
            }
            
            TempData["SuccessMessage"] = "Lecturer account deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Edit lecturer account
        public async Task<IActionResult> EditLecturer(string id)
        {
            var lecturer = await _userManager.FindByIdAsync(id);
            
            if (lecturer == null)
            {
                return NotFound();
            }
            
            var model = new EditLecturerViewModel
            {
                Id = lecturer.Id,
                Email = lecturer.Email,
                UserName = lecturer.UserName
            };
            
            return View(model);
        }
        
        // POST: Edit lecturer account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(EditLecturerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            var lecturer = await _userManager.FindByIdAsync(model.Id);
            
            if (lecturer == null)
            {
                return NotFound();
            }
            
            // Track if email changed
            bool emailChanged = lecturer.Email != model.Email;
            string oldEmail = lecturer.Email;
            
            // Update the lecturer properties
            lecturer.Email = model.Email;
            lecturer.UserName = model.Email; // Keep email and username in sync
            lecturer.EmailConfirmed = true;
            
            var result = await _userManager.UpdateAsync(lecturer);
            
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            
            // If password is provided, update it
            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(lecturer);
                var resetResult = await _userManager.ResetPasswordAsync(lecturer, token, model.Password);
                
                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            
            // Notify the lecturer if their account was updated
            await _notificationService.CreateNotificationAsync(
                lecturer.Id,
                "Account Updated",
                $"Your account details have been updated by an administrator.",
                "/Lecturer/Dashboard",
                NotificationType.System
            );
            
            // If email was changed, add that to the notification
            if (emailChanged)
            {
                await _notificationService.CreateNotificationAsync(
                    lecturer.Id,
                    "Email Address Changed",
                    $"Your email address has been changed from {oldEmail} to {model.Email}.",
                    "/Lecturer/Dashboard",
                    NotificationType.System
                );
            }
            
            TempData["SuccessMessage"] = "Lecturer account updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class EditLecturerViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Username")]
        public string UserName { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0} must be at least {2} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password (leave blank to keep current)")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

    }
}