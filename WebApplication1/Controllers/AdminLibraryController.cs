using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLibraryController : Controller
    {
        private readonly NexelContext _context;

        public AdminLibraryController(NexelContext context)
        {
            _context = context;
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
                TempData["SuccessMessage"] = "Book added successfully!";
                return RedirectToAction(nameof(Index));
            
            return View(resource);
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
            var booking = await _context.ResourceBookings.FindAsync(bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Generate a random 6-digit PIN
            booking.ReturnPin = GeneratePin();
            await _context.SaveChangesAsync();

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
            TempData["SuccessMessage"] = "Resource return confirmed successfully!";
            return RedirectToAction(nameof(ActiveBookings));
        }

        // Helper method to generate a 6-digit PIN
        private string GeneratePin()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }
    }
}