using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{

    public class CoursesController : Controller
    {
        private readonly NexelContext _context;

        public CoursesController(NexelContext context)
        {
            _context = context;
        }

        // GET: Courses/Browse
        public async Task<IActionResult> Browse()
        {
            // Fetch all courses with their modules
            var courses = await _context.Courses
                .Include(c => c.Modules)
                .OrderBy(c => c.Faculty)
                .ThenBy(c => c.CourseName)
                .ToListAsync();

            // Group courses by faculty
            var viewModel = new CoursesByFacultyViewModel();

            foreach (var course in courses)
            {
                if (!viewModel.CoursesByFaculty.ContainsKey(course.Faculty))
                {
                    viewModel.CoursesByFaculty[course.Faculty] = new List<Course>();
                }

                viewModel.CoursesByFaculty[course.Faculty].Add(course);
            }

            return View(viewModel);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
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
    }
}