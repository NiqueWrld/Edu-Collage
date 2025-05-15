using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly NexelContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(NexelContext context , UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Courses()
        {
            return View(await _context.Courses.ToListAsync());
        }

        // View All Courses
        public async Task<IActionResult> AdminDashboard()
        {
            return View();
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

        public async Task<IActionResult> EditModule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var module = await _context.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.CourseId == id);

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
        public async Task<IActionResult> EditModule(int id, [Bind("ModuleId,ModuleName,ModuleCode,Year,Semester,CourseId")] Module @module)
        {
            if (id != @module.ModuleId)
            {
                return NotFound();
            }

                try
                {
                    _context.Update(@module);
                    await _context.SaveChangesAsync();

                return RedirectToAction(nameof(CourseDetails), new { id = module.CourseId });
            }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ModuleExists(@module.ModuleId))
                    {
                        return NotFound();
                    }
                    else
                    {
                    return RedirectToAction(nameof(CourseDetails), new { id = module.CourseId });
                }
                }
             
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
            }
            else if (decision.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                application.Status = Application.ApplicationStatus.Rejected;
                application.RejectedDate = DateTime.UtcNow;
                application.ApprovedDate = null; // Clear approval date if previously set
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
        public async Task<IActionResult> AddModule([Bind("ModuleId,ModuleName,Description,ModuleCode,Year,Semester,CourseId")] Module module)
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
