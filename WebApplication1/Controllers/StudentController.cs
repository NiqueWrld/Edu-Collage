using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Braintree;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly NexelContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<StudentController> _logger;
        private readonly BraintreeGateway _braintreeGateway;

        public StudentController(NexelContext context, UserManager<IdentityUser> userManager,  IConfiguration configuration, BraintreeGateway braintreeGateway , ILogger<StudentController> logger = null )
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _braintreeGateway = braintreeGateway;

            try
            {
                var account = new Account(
                    _configuration["Cloudinary:CloudName"],
                    _configuration["Cloudinary:ApiKey"],
                    _configuration["Cloudinary:ApiSecret"]);

                _cloudinary = new Cloudinary(account);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to initialize Cloudinary: {ex.Message}");
            }
        }

      

        public async Task<IActionResult> ProceedToPayment(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != Application.ApplicationStatus.Approved)
            {
                TempData["ErrorMessage"] = "You can only proceed to payment for approved applications.";
                return RedirectToAction("TrackApplications");
            }

            var clientToken = await _braintreeGateway.ClientToken.GenerateAsync();

            ViewData["ClientToken"] = clientToken;

            var paymentModel = new PaymentViewModel
            {
                ApplicationId = application.ApplicationId,
                CourseName = application.Course?.CourseName,
                ApplicationFee = application.ApplicationFee,
                StudyYear = application.StudyYear
            };

            return View(paymentModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment(int applicationId, string paymentNonce)
        {
            var application = await _context.Applications.FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != Application.ApplicationStatus.Approved)
            {
                TempData["ErrorMessage"] = "Payment can only be made for approved applications.";
                return RedirectToAction("TrackApplications");
            }

            // Create a transaction request
            var request = new TransactionRequest
            {
                Amount = application.ApplicationFee,
                PaymentMethodNonce = paymentNonce,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };

            var result = await _braintreeGateway.Transaction.SaleAsync(request);

            if (result.IsSuccess())
            {
                // Create a new Payment record
                var payment = new Payment
                {
                    PaymentDate = DateTime.UtcNow,
                    Amount = application.ApplicationFee,
                    PaymentMethod = "Credit Card", // You can customize this based on the payment method used
                    Status = "Completed",
                    IdentityUserId = application.IdentityUserId // Assuming `IdentityUserId` is a field in Application
                };

                // Save the Payment record to the database
                _context.Payments.Add(payment);

                // Update the application to reflect payment status
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment completed successfully!";
                return RedirectToAction("TrackApplications");
            }
            else
            {
                TempData["ErrorMessage"] = $"Payment failed: {result.Message}";
                return RedirectToAction("ProceedToPayment", new { id = applicationId });
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ApplyAdmission()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseCode");
            ViewData["IdentityUserId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "IdentityUserId");
            ViewData["ProcessedByUserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyAdmission(Application application, IFormFile IdentificationDocumentPath, IFormFile AcademicRecordsPath, IFormFile MotivationLetterPath)
        {
            var cloudinary = new Cloudinary(new Account(
                "dtj6g1nt2",
                "871839689527671",
                "FR9B7olkGgn6fxfEjmHBbA03qxE"
            ));

            async Task<string> UploadToCloudinary(IFormFile file)
            {
                using var stream = file.OpenReadStream();

                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = file.FileName,
                    Folder = "applications/",
                    Type = "upload"  // Important to ensure it's public
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.ToString();
            }


            if (IdentificationDocumentPath != null)
                application.IdentificationDocumentPath = await UploadToCloudinary(IdentificationDocumentPath);

            if (AcademicRecordsPath != null)
                application.AcademicRecordsPath = await UploadToCloudinary(AcademicRecordsPath);

            if (MotivationLetterPath != null)
                application.MotivationLetterPath = await UploadToCloudinary(MotivationLetterPath);

            application.ApplicationDate = DateTime.Now;
            application.Status = Application.ApplicationStatus.Pending;
            application.IdentityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            // Save application to database
            _context.Add(application);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TrackApplications));
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> TrackApplications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "You must be logged in to view your applications.";
                    return RedirectToAction("Login", "Account");
                }

                var applications = await _context.Applications
                    .Include(a => a.Course)
                    .Where(a => a.IdentityUserId == user.Id)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToListAsync();

                return View(applications);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving applications: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to retrieve your applications. Please try again later.";
                return View(new List<Application>());
            }
        }

       
    }
}