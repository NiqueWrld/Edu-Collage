using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class StudentController : Controller
    {

        private readonly NexelContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(NexelContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ApplyAdmission()
        {
            ViewBag.Courses = _context.Courses.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyAdmission(Application application)
        {
            if (ModelState.IsValid)
            {
                // Get the current logged-in user
                var user = await _userManager.GetUserAsync(User);
                application.IdentityUserId = user.Id;
                application.ApplicationDate = DateTime.UtcNow;
                application.Status = Application.ApplicationStatus.Pending; // Default status

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your application has been submitted successfully.";
                return RedirectToAction("TrackApplication");
            }

            ViewBag.Courses = _context.Courses.ToList(); // Re-populate courses for dropdown in case of error
            return View(application);
        }

    }
}
