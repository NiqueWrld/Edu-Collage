using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ModuleController : Controller
    {
        private readonly NexelContext _context;

        public ModuleController(NexelContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Module module)
        {
            if (ModelState.IsValid)
            {
                _context.Modules.Add(module);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Course");
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", module.CourseId);
            return View(module);
        }
    }
}
