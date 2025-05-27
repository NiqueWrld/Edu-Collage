using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Student")]
    public class LibraryController : Controller
    {
        private readonly NexelContext _context;
        private readonly NotificationService _notificationService;

        public LibraryController(NexelContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: Library home page with options
        public IActionResult Index()
        {
            return View();
        }

        // GET: View available books
        public async Task<IActionResult> AvailableBooks()
        {
            var books = await _context.LibraryResources
                .Where(r => r.Type == ResourceType.Book && r.IsAvailable)
                .ToListAsync();

            return View(books);
        }

        // GET: View available computers
        public async Task<IActionResult> AvailableComputers()
        {
            var computers = await _context.LibraryResources
                .Where(r => r.Type == ResourceType.Computer && r.IsAvailable)
                .ToListAsync();

            return View(computers);
        }

        // GET: View all resources together
        public async Task<IActionResult> BookResources()
        {
            var viewModel = new BookResourcesViewModel
            {
                Books = await _context.LibraryResources
                    .Where(r => r.Type == ResourceType.Book && r.IsAvailable)
                    .ToListAsync(),

                Computers = await _context.LibraryResources
                    .Where(r => r.Type == ResourceType.Computer && r.IsAvailable)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Book a resource
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookResource(int resourceId)
        {
            var resource = await _context.LibraryResources.FindAsync(resourceId);

            if (resource == null)
            {
                return NotFound();
            }

            if (!resource.IsAvailable)
            {
                TempData["ErrorMessage"] = "This resource is no longer available.";
                return RedirectToAction(nameof(BookResources));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Random random = new Random();

            var booking = new ResourceBooking
            {
                ReturnPin = random.Next(1000, 10000).ToString(),
                CollectionPin = random.Next(1000, 10000).ToString(),
                ResourceId = resourceId,
                IdentityUserId = userId,
                BookingDate = DateTime.UtcNow,
                Status = BookingStatus.Active
            };

            // Calculate due date based on resource type
            booking.DueDate = resource.Type == ResourceType.Book ?
                booking.BookingDate.AddDays(7) :
                booking.BookingDate.AddHours(1);

            resource.IsAvailable = false;

            _context.ResourceBookings.Add(booking);
            await _context.SaveChangesAsync();

            // Send notification to the user about successful booking
            await _notificationService.CreateNotificationAsync(
                userId,
                "Resource Booked Successfully",
                $"You have booked {resource.Name} until {booking.DueDate.ToString("yyyy-MM-dd HH:mm")}. Your return PIN is {booking.ReturnPin}.",
                $"/Library/MyBookings",
                NotificationType.General
            );

            // If due date is for a book (longer period), create a reminder notification for 2 days before due date
            if (resource.Type == ResourceType.Book)
            {
                // Create a reminder task that will be triggered 2 days before the due date
                // This is a simplified implementation - in a real app, you might use a background job scheduler
                var reminderDate = booking.DueDate.AddDays(-2);
                if (reminderDate > DateTime.UtcNow)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId,
                        "Book Return Reminder",
                        $"Your book '{resource.Name}' is due to be returned in 2 days. Please ensure timely return to avoid late fees.",
                        $"/Library/MyBookings",
                        NotificationType.General
                    );
                }
            }

            TempData["SuccessMessage"] = $"You have successfully booked this {(resource.Type == ResourceType.Book ? "book" : "computer")}!";
            return RedirectToAction(nameof(MyBookings));
        }

        // GET: View student's current bookings
        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bookings = await _context.ResourceBookings
                .Include(b => b.Resource)
                .Where(b => b.IdentityUserId == userId && b.Status != BookingStatus.Returned)
                .ToListAsync();

            // Check for overdue resources and send notifications if not already notified
            foreach (var booking in bookings)
            {
                if (booking.DueDate < DateTime.UtcNow && booking.Status == BookingStatus.Active)
                {
                    // Update status to overdue
                    booking.Status = BookingStatus.Overdue;

                    // Create overdue notification if not already created
                    await _notificationService.CreateNotificationAsync(
                        userId,
                        "Resource Overdue",
                        $"Your {booking.Resource.Type} '{booking.Resource.Name}' is overdue. Please return it as soon as possible to avoid additional fees.",
                        $"/Library/ReturnResource/{booking.BookingId}",
                        NotificationType.General
                    );
                }
            }

            if (bookings.Any(b => b.Status == BookingStatus.Overdue))
            {
                await _context.SaveChangesAsync();
            }

            return View(bookings);
        }

        // GET: Return a resource (show PIN entry form)
        public async Task<IActionResult> ReturnResource(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.ResourceBookings
                .Include(b => b.Resource)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.IdentityUserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Submit PIN to return a resource
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReturn(int bookingId, string returnPin)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.ResourceBookings
                .Include(b => b.Resource)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.IdentityUserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.ReturnPin != returnPin)
            {
                TempData["ErrorMessage"] = "Invalid PIN. Please ask the librarian for the correct return PIN.";
                return RedirectToAction(nameof(ReturnResource), new { bookingId });
            }

            booking.Status = BookingStatus.Returned;
            booking.ReturnDate = DateTime.UtcNow;
            booking.Resource.IsAvailable = true;

            await _context.SaveChangesAsync();

            // Send confirmation notification
            await _notificationService.CreateNotificationAsync(
                userId,
                "Resource Returned Successfully",
                $"Thank you for returning {booking.Resource.Name}. The resource has been marked as returned.",
                $"/Library/MyBookings",
                NotificationType.General
            );

            TempData["SuccessMessage"] = "Resource returned successfully!";
            return RedirectToAction(nameof(MyBookings));
        }

    }
}
