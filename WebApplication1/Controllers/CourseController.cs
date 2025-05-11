using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CourseController : Controller
    {
        private readonly NexelContext _context;

        public CourseController(NexelContext context)
        {
            _context = context;
        }

        private static List<string> faculties = new List<string>
    {
        "FACULTY OF ARTS AND DESIGN",
        "FACULTY OF ACCOUNTING AND INFORMATICS",
        "FACULTY OF MANAGEMENT SCIENCES",
        "FACULTY OF APPLIED SCIENCES",
        "FACULTY OF ENGINEERING AND THE BUILT ENVIRONMENT",
        "FACULTY OF HEALTH SCIENCES",
        "GENERAL EDUCATION"
    };

        private static List<Course> courses = new List<Course>();

        public IActionResult Index()
        {
            ViewBag.Faculties = faculties;
            return View(courses);
        }

        public IActionResult Create()
        {
            ViewBag.Faculties = faculties;
            return View();
        }

        [HttpPost]
        public IActionResult Create(Course course)
        {
            if (ModelState.IsValid)
            {
                courses.Add(course);
                return RedirectToAction("Index");
            }

            ViewBag.Faculties = faculties;
            return View(course);
        }

        public IActionResult ManageCourse()
        {
            return View(courses);
        }
    }
}
