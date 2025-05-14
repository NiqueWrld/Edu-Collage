using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Student")]
    public class LibraryController : Controller
    {
        private readonly NexelContext _context;

        public LibraryController(NexelContext context)
        {
            _context = context;
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
            TempData["SuccessMessage"] = "Resource returned successfully!";
            return RedirectToAction(nameof(MyBookings));
        }
    }
}