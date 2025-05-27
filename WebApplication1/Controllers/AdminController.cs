using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly NexelContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotificationService _notificationService;

        public AdminController(NexelContext context, UserManager<IdentityUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }


        public async Task<IActionResult> Courses()
        {
            return View(await _context.Courses.ToListAsync());
        }

        // View All Courses
        public async Task<IActionResult> AdminDashboard()
        {
            var totalCourses = await _context.Courses.CountAsync();
            var studentRole = await _context.Roles
     .Where(r => r.Name == "Student")
     .Select(r => r.Id)
     .FirstOrDefaultAsync();

            var totalStudents = await _context.UserRoles
                .Where(ur => ur.RoleId == studentRole)
                .CountAsync();

            var totalApplications = await _context.Applications.CountAsync();
            var lecturesThisWeek = await _context.ModuleLecturers
                .Where(m => m.AssignedDate >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalCourses = totalCourses,
                TotalStudents = totalStudents,
                TotalApplications = totalApplications,
                LecturesThisWeek = lecturesThisWeek,
                RecentApplications = await _context.Applications
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public class AdminDashboardViewModel
        {
            public int TotalCourses { get; set; }
            public int TotalStudents { get; set; }
            public int TotalApplications { get; set; }
            public int LecturesThisWeek { get; set; }
            public List<Application> RecentApplications { get; set; }
        }

        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse([Bind("Id,Faculty,CourseName,CourseCode,Description,DurationYears")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Courses));
            }
            return View(course);
        }

        public async Task<IActionResult> CourseDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        public async Task<IActionResult> AddModule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            ViewBag.CourseId = course.Id;
            ViewBag.CourseYears = course.DurationYears;

            return View();
        }

        public async Task<IActionResult> EditModule(int id)
        {

            var module = await _context.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.ModuleId == id);

            if (module == null)
            {
                return NotFound();
            }

            ViewBag.CourseId = module.CourseId;
            ViewBag.CourseYears = module.Course.DurationYears;

            return View(module);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModule(int id, [Bind("ModuleId,ModuleName,ModuleCode,ModulePrice,Description,Year,Semester,CourseId,ClassDay,ClassTime,ModuleVenue")] Module module)
        {
            if (id != module.ModuleId)
            {
                return NotFound();
            }

            
                try
                {
                    _context.Update(module);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(CourseDetails), new { id = module.CourseId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ModuleExists(module.ModuleId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            
            // If not valid, re-populate ViewBag and return view
            var course = await _context.Courses.FindAsync(module.CourseId);
            ViewBag.CourseId = module.CourseId;
            ViewBag.CourseYears = course?.DurationYears ?? 1;
            return View(module);
        }


        public async Task<IActionResult> Applications()
        {
            var nexelContext = _context.Applications.Include(a => a.Course).Include(a => a.IdentityUser).Include(a => a.Payment).Include(a => a.ProcessedByUser);
            return View(await nexelContext.ToListAsync());
        }

        public async Task<IActionResult> Review(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.Course)
                .Include(a => a.IdentityUser)
                .Include(a => a.Payment)
                .Include(a => a.ProcessedByUser)
                .FirstOrDefaultAsync(m => m.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            application.Status = Application.ApplicationStatus.UnderReview;
            await _context.SaveChangesAsync();

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideApplication(int applicationId, string decision, string adminComments)
        {
            if (string.IsNullOrEmpty(decision))
            {
                ModelState.AddModelError("", "A decision must be made.");
                return RedirectToAction("Details", new { id = applicationId });
            }

            var application = await _context.Applications
                .Include(a => a.IdentityUser)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            // Get the current logged-in admin
            var admin = await _userManager.GetUserAsync(User);

            // Update application details based on decision
            application.AdminComments = adminComments;
            application.ReviewedDate = DateTime.UtcNow;
            application.ProcessedByUserId = admin.Id;

            if (decision.Equals("approve", StringComparison.OrdinalIgnoreCase))
            {
                application.Status = Application.ApplicationStatus.Approved;
                application.ApprovedDate = DateTime.UtcNow;
                application.RejectedDate = null; // Clear rejection date if previously set

                await _notificationService.CreateNotificationAsync(
        application.IdentityUserId,
        "Application Approved",
        $"Your application for {application.Course.CourseName} has been approved.",
        $"/Student/Applications/Details/{application.ApplicationId}",
        NotificationType.General
    );

            }
            else if (decision.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                application.Status = Application.ApplicationStatus.Rejected;
                application.RejectedDate = DateTime.UtcNow;
                application.ApprovedDate = null; // Clear approval date if previously set

                await _notificationService.CreateNotificationAsync(
        application.IdentityUserId,
        "Application Rejected",
        $"Your application for {application.Course.CourseName} has been rejected.",
        $"/Student/Applications/Details/{application.ApplicationId}",
        NotificationType.General
    );

            }
            else
            {
                ModelState.AddModelError("", "Invalid decision.");
                return RedirectToAction("Details", new { id = applicationId });
            }

            try
            {
                // Save changes to the database
                _context.Applications.Update(application);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Application has been successfully {decision}d.";
                return RedirectToAction(nameof(Applications));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while processing the application: {ex.Message}");
                return RedirectToAction("Details", new { id = applicationId });
            }
        }

        public async Task<IActionResult> DeleteModule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @module = await _context.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.ModuleId == id);
            if (@module == null)
            {
                return NotFound();
            }

            return View(@module);
        }

        [HttpPost, ActionName("DeleteModule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModuleConfirmed(int id)
        {
            var @module = await _context.Modules.FindAsync(id);
            if (@module != null)
            {
                _context.Modules.Remove(@module);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CourseDetails), new { id = module.CourseId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModule([Bind("ModuleId,ModuleName,ModuleCode,ModulePrice,Description,Year,Semester,CourseId,ClassDay,ClassTime,ModuleVenue")] Module module)
        {
            _context.Add(module);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CourseDetails), new { id = module.CourseId });
        }


        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, [Bind("Id,Faculty,CourseName,CourseCode,Description,DurationYears")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Courses));
            }
            return View(course);
        }

        public async Task<IActionResult> ModuleDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @module = await _context.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.ModuleId == id);
            if (@module == null)
            {
                return NotFound();
            }

            return View(@module);
        }

        public async Task<IActionResult> DeleteCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Courses));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        private bool ModuleExists(int id)
        {
            return _context.Modules.Any(e => e.ModuleId == id);
        }

    }
}
