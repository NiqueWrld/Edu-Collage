using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly NexelContext _context;

        public ApplicationsController(NexelContext context)
        {
            _context = context;
        }

        // GET: Applications
        public async Task<IActionResult> Index()
        {
            var nexelContext = _context.Applications.Include(a => a.Course).Include(a => a.IdentityUser).Include(a => a.Payment).Include(a => a.ProcessedByUser);
            return View(await nexelContext.ToListAsync());
        }

        // GET: Applications/Details/5
        public async Task<IActionResult> Details(int? id)
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

            return View(application);
        }

        // GET: Applications/Create
   

        // GET: Applications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseCode", application.CourseId);
            ViewData["IdentityUserId"] = new SelectList(_context.Users, "Id", "Id", application.IdentityUserId);
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "IdentityUserId", application.PaymentId);
            ViewData["ProcessedByUserId"] = new SelectList(_context.Users, "Id", "Id", application.ProcessedByUserId);
            return View(application);
        }

        // POST: Applications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicationId,ApplicationDate,Status,ReviewedDate,ApprovedDate,RejectedDate,FirstName,LastName,PhoneNumber,DateOfBirth,Address,CourseId,StudyYear,IdentificationDocumentPath,AcademicRecordsPath,MotivationLetterPath,ApplicationFee,PaymentId,PaymentRequired,IdentityUserId,AdminComments,ProcessedByUserId")] Application application)
        {
            if (id != application.ApplicationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationExists(application.ApplicationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseCode", application.CourseId);
            ViewData["IdentityUserId"] = new SelectList(_context.Users, "Id", "Id", application.IdentityUserId);
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "IdentityUserId", application.PaymentId);
            ViewData["ProcessedByUserId"] = new SelectList(_context.Users, "Id", "Id", application.ProcessedByUserId);
            return View(application);
        }

        // GET: Applications/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            return View(application);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application != null)
            {
                _context.Applications.Remove(application);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.ApplicationId == id);
        }
    }
}
