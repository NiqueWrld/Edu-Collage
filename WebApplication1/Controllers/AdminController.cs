using Microsoft.AspNetCore.Authorization;
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

        public AdminController(NexelContext context)
        {
            _context = context;
        }

        // View All Courses
        public async Task<IActionResult> Courses()
        {
            return View(await _context.Courses.ToListAsync());
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
        public async Task<IActionResult> AddModule([Bind("ModuleId,ModuleName,ModuleCode,Year,Semester,CourseId")] Module module)
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
