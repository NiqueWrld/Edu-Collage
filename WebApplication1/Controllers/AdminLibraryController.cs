using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLibraryController : Controller
    {
        private readonly NexelContext _context;
        private readonly NotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminLibraryController(
            NexelContext context,
            NotificationService notificationService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: Display list of all library resources
        public async Task<IActionResult> Index()
        {
            var resources = await _context.LibraryResources.ToListAsync();
            return View(resources);
        }

        // GET: Form to add a new book
        public IActionResult AddBook()
        {
            return View();
        }

        // POST: Process form submission to add a book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(LibraryResource resource)
        {
            resource.Type = ResourceType.Book;
            _context.LibraryResources.Add(resource);
            await _context.SaveChangesAsync();

            // Notify students who have library interests
            // This would typically be users with a specific role or preference setting
            var studentsWithLibraryInterest = await _userManager.GetUsersInRoleAsync("Student");

            if (studentsWithLibraryInterest.Any())
            {
                var studentIds = studentsWithLibraryInterest.Select(u => u.Id).ToList();

                await _notificationService.CreateBulkNotificationsAsync(
                    studentIds,
                    "New Book Available",
                    $"A new book '{resource.Name}' by {resource.Author} is now available in the library.",
                    "/Library/AvailableBooks",
                    NotificationType.General
                );
            }

            TempData["SuccessMessage"] = "Book added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Form to add a new PC
        public IActionResult AddComputer()
        {
            return View();
        }

        // POST: Process form submission to add a PC
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComputer(LibraryResource resource)
        {
            resource.Type = ResourceType.Computer;
            _context.LibraryResources.Add(resource);
            await _context.SaveChangesAsync();

            // Notify students who have library interests
            var studentsWithLibraryInterest = await _userManager.GetUsersInRoleAsync("Student");

            if (studentsWithLibraryInterest.Any())
            {
                var studentIds = studentsWithLibraryInterest.Select(u => u.Id).ToList();

                await _notificationService.CreateBulkNotificationsAsync(
                    studentIds,
                    "New Computer Available",
                    $"A new computer '{resource.Name}' ({resource.Specifications}) is now available in the library at {resource.Location}.",
                    "/Library/AvailableComputers",
                    NotificationType.General
                );
            }

            TempData["SuccessMessage"] = "Computer added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: View all active bookings
        public async Task<IActionResult> ActiveBookings()
        {
            var bookings = await _context.ResourceBookings
                .Include(b => b.Resource)
                .Include(b => b.IdentityUser)
                .Where(b => b.Status == BookingStatus.Active)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Generate return PIN for a booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReturnPin(int bookingId)
        {
            var booking = await _context.ResourceBookings
                .Include(b => b.Resource)
                .Include(b => b.IdentityUser)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Generate a random 6-digit PIN
            booking.ReturnPin = GeneratePin();
            await _context.SaveChangesAsync();

            // Notify student about the generated PIN
            await _notificationService.CreateNotificationAsync(
                booking.IdentityUserId,
                "Return PIN Generated",
                $"A return PIN has been generated for your {booking.Resource.Type} '{booking.Resource.Name}'. PIN: {booking.ReturnPin}",
                $"/Library/ReturnResource/{booking.BookingId}",
                NotificationType.General
            );

            TempData["SuccessMessage"] = $"Return PIN generated: {booking.ReturnPin}";
            return RedirectToAction(nameof(ActiveBookings));
        }

        // POST: Confirm resource return using PIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturn(int bookingId, string enteredPin)
        {
            var booking = await _context.ResourceBookings
                .Include(b => b.Resource)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.ReturnPin != enteredPin)
            {
                TempData["ErrorMessage"] = "Invalid PIN. Please try again.";
                return RedirectToAction(nameof(ActiveBookings));
            }

            booking.Status = BookingStatus.Returned;
            booking.ReturnDate = DateTime.UtcNow;
            booking.Resource.IsAvailable = true;

            await _context.SaveChangesAsync();

            // Notify student that the resource has been returned
            await _notificationService.CreateNotificationAsync(
                booking.IdentityUserId,
                "Resource Return Confirmed",
                $"Your {booking.Resource.Type} '{booking.Resource.Name}' has been successfully returned and marked as available.",
                "/Library/MyBookings",
                NotificationType.General
            );

            TempData["SuccessMessage"] = "Resource return confirmed successfully!";
            return RedirectToAction(nameof(ActiveBookings));
        }

        // GET: Delete a resource
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.LibraryResources
                .Include(r => r.Bookings)
                .ThenInclude(b => b.IdentityUser)
                .FirstOrDefaultAsync(r => r.ResourceId == id);

            if (resource == null)
            {
                return NotFound();
            }

            // Check if resource has active bookings
            if (resource.Bookings != null && resource.Bookings.Any(b => b.Status == BookingStatus.Active || b.Status == BookingStatus.Overdue))
            {
                TempData["ErrorMessage"] = "Cannot delete resource with active bookings.";
                return RedirectToAction(nameof(Index));
            }

            return View(resource);
        }

        // POST: Delete a resource
        [HttpPost, ActionName("DeleteResource")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResourceConfirmed(int id)
        {
            var resource = await _context.LibraryResources.FindAsync(id);

            if (resource == null)
            {
                return NotFound();
            }

            // Notify administrators about resource deletion
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var otherAdminIds = adminUsers.Where(a => a.Id != currentUserId).Select(a => a.Id).ToList();

            if (otherAdminIds.Any())
            {
                await _notificationService.CreateBulkNotificationsAsync(
                    otherAdminIds,
                    "Library Resource Deleted",
                    $"The {resource.Type} '{resource.Name}' has been removed from the library inventory.",
                    "/AdminLibrary/Index",
                    NotificationType.System
                );
            }

            _context.LibraryResources.Remove(resource);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{resource.Type} '{resource.Name}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: View overdue resources
        public async Task<IActionResult> OverdueResources()
        {
            var overdueBookings = await _context.ResourceBookings
                .Include(b => b.Resource)
                .Include(b => b.IdentityUser)
                .Where(b => b.Status == BookingStatus.Overdue)
                .ToListAsync();

            return View(overdueBookings);
        }

        // POST: Send reminder to student with overdue resource
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(int bookingId)
        {
            var booking = await _context.ResourceBookings
                .Include(b => b.Resource)
                .Include(b => b.IdentityUser)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Send a reminder notification
            await _notificationService.CreateNotificationAsync(
                booking.IdentityUserId,
                "Overdue Resource Reminder",
                $"Reminder: Your {booking.Resource.Type} '{booking.Resource.Name}' is overdue. Please return it as soon as possible to avoid additional fees.",
                $"/Library/ReturnResource/{booking.BookingId}",
                NotificationType.System
            );

            TempData["SuccessMessage"] = "Reminder sent successfully!";
            return RedirectToAction(nameof(OverdueResources));
        }

        // Helper method to generate a 6-digit PIN
        private string GeneratePin()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }
    }
}
