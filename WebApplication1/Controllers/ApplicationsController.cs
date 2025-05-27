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
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly NexelContext _context;
        private readonly NotificationService _notificationService;
        TimeZoneInfo southAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

        public ApplicationsController(NexelContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicationId,ApplicationDate,Status,ReviewedDate,ApprovedDate,RejectedDate,FirstName,LastName,PhoneNumber,DateOfBirth,Address,CourseId,StudyYear,IdentificationDocumentPath,AcademicRecordsPath,MotivationLetterPath,ApplicationFee,PaymentId,PaymentRequired,IdentityUserId,AdminComments,ProcessedByUserId")] Application application)
        {
            if (id != application.ApplicationId)
            {
                return NotFound();
            }

            // Get the original application to detect status changes
            var originalApplication = await _context.Applications
                .Include(a => a.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for status changes to send notifications
                    bool statusChanged = originalApplication.Status != application.Status;

                    if (statusChanged)
                    {
                        var course = await _context.Courses.FindAsync(application.CourseId);
                        string courseName = course?.CourseName ?? "the selected course";

                        switch (application.Status)
                        {
                            case Application.ApplicationStatus.UnderReview:
                                // Notify student that application is under review
                                await _notificationService.CreateNotificationAsync(
                                    application.IdentityUserId,
                                    "Application Under Review",
                                    $"Your application for {courseName} is now being reviewed.",
                                    $"/Student/TrackApplications",
                                    NotificationType.System
                                );
                                application.ReviewedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);
                                break;

                            case Application.ApplicationStatus.Approved:
                                // Notify student that application is approved
                                await _notificationService.CreateNotificationAsync(
                                    application.IdentityUserId,
                                    "Application Approved",
                                    $"Your application for {courseName} has been approved! Please proceed to payment.",
                                    $"/Student/ProceedToPayment/{application.ApplicationId}",
                                    NotificationType.System
                                );
                                application.ApprovedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);
                                break;

                            case Application.ApplicationStatus.Rejected:
                                // Notify student that application is rejected
                                await _notificationService.CreateNotificationAsync(
                                    application.IdentityUserId,
                                    "Application Rejected",
                                    $"Your application for {courseName} was not approved. Please check the comments for more information.",
                                    $"/Student/TrackApplications",
                                    NotificationType.System
                                );
                                application.RejectedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);
                                break;
                        }
                    }

                    // If admin comments were added or changed, notify the student
                    if (!string.IsNullOrEmpty(application.AdminComments) &&
                        application.AdminComments != originalApplication.AdminComments)
                    {
                        await _notificationService.CreateNotificationAsync(
                            application.IdentityUserId,
                            "Application Updated",
                            "Your application has been updated with new comments from the administration.",
                            $"/Student/TrackApplications",
                            NotificationType.System
                        );
                    }

                    _context.Update(application);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Application updated successfully.";
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
                // Notify student that application was deleted
                await _notificationService.CreateNotificationAsync(
                    application.IdentityUserId,
                    "Application Deleted",
                    "Your application has been deleted by the administration.",
                    "/Student/TrackApplications",
                    NotificationType.System
                );

                _context.Applications.Remove(application);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Application deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.ApplicationId == id);
        }

        // POST: Applications/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, Application.ApplicationStatus status, string comments = null)
        {
            var application = await _context.Applications
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            var course = application.Course;
            string courseName = course?.CourseName ?? "the selected course";

            // Update status
            application.Status = status;

            // Update relevant dates
            switch (status)
            {
                case Application.ApplicationStatus.UnderReview:
                    application.ReviewedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);
                    break;
                case Application.ApplicationStatus.Approved:
                    application.ApprovedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);
                    break;  
                case Application.ApplicationStatus.Rejected:
                    application.RejectedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);
                    break;
            }

            // Update admin comments if provided
            if (!string.IsNullOrEmpty(comments))
            {
                application.AdminComments = comments;
            }

            // Add the current user as the processor
            application.ProcessedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Send notification
            string notificationTitle;
            string notificationMessage;
            string notificationLink;

            switch (status)
            {
                case Application.ApplicationStatus.UnderReview:
                    notificationTitle = "Application Under Review";
                    notificationMessage = $"Your application for {courseName} is now being reviewed.";
                    notificationLink = "/Student/TrackApplications";
                    break;
                case Application.ApplicationStatus.Approved:
                    notificationTitle = "Application Approved";
                    notificationMessage = $"Your application for {courseName} has been approved! Please proceed to payment.";
                    notificationLink = $"/Student/ProceedToPayment/{application.ApplicationId}";
                    break;
                case Application.ApplicationStatus.Rejected:
                    notificationTitle = "Application Rejected";
                    notificationMessage = $"Your application for {courseName} was not approved. Please check the comments for more information.";
                    notificationLink = "/Student/TrackApplications";
                    break;
                default:
                    notificationTitle = "Application Status Updated";
                    notificationMessage = $"The status of your application for {courseName} has been updated.";
                    notificationLink = "/Student/TrackApplications";
                    break;
            }

            await _notificationService.CreateNotificationAsync(
                application.IdentityUserId,
                notificationTitle,
                notificationMessage,
                notificationLink,
                NotificationType.System
            );

            _context.Update(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application status updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
