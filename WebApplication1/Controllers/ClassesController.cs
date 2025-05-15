using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Student")]
    public class ClassesController : Controller
    {
        private readonly NexelContext _context;

        public ClassesController(NexelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the student's approved and paid application
            var application = await _context.Applications
                .Include(a => a.Course)
                .ThenInclude(c => c.Modules)
                .FirstOrDefaultAsync(a =>
                    a.IdentityUserId == userId &&
                    a.Status == Models.Application.ApplicationStatus.Approved &&
                    a.PaymentId != null);

            if (application == null)
            {
                // The student hasn't completed enrollment yet
                return View("NotEnrolled");
            }

            // First, calculate what year of study the student is currently in
            int yearStarted = int.Parse(application.StudyYear); // Year student started
            int currentYear = DateTime.UtcNow.Year; // Current calendar year
            int currentYearOfStudy = currentYear - yearStarted + 1; // Calculate current year of study

            // Ensure the calculated year doesn't exceed the course duration
            currentYearOfStudy = Math.Min(currentYearOfStudy, application.Course.DurationYears);

            var viewModel = new ClassesViewModel
            {
                StudentName = User.Identity.Name,
                Course = application.Course,
                StudyYear = currentYearOfStudy,
                // Filter modules that are appropriate for the student's current year of study
                EnrolledModules = application.Course.Modules
                    .Where(m => m.Year == currentYearOfStudy.ToString()) // Module year should match current study year
                    .OrderBy(m => m.Semester)
                    .ThenBy(m => m.ModuleName)
                    .ToList()
            };

            return View(viewModel);
        }

        public IActionResult ModuleChat(int id)
        {
            var module = _context.Modules
                .Include(m => m.Course)
                .FirstOrDefault(m => m.ModuleId == id);

            if (module == null)
            {
                return NotFound();
            }

            ViewBag.ModuleId = module.ModuleId;
            ViewBag.ModuleName = module.ModuleName;
            ViewBag.CourseCode = module.Course.CourseCode;

            return View();
        }
    }
}