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
using WebApplication1.Models.ViewModels;

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
                    PaymentMethod = "Credit Card",
                    Status = "Completed",
                    IdentityUserId = application.IdentityUserId 
                };


                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(); 

              
                application.PaymentId = payment.PaymentId;

                // Save the updated application
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

        public async Task<IActionResult> ModuleDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            

            var module = await _context.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.ModuleId == id);

            if (module == null)
            {
                return NotFound();
            }

            // Get study materials for this module
            var studyMaterials = await _context.StudyMaterials
                .Where(sm => sm.ModuleId == id)
                .OrderByDescending(sm => sm.UploadedDate)
                .ToListAsync();

            // Get quizzes for this module
            var quizzes = await _context.Quizzes
                .Where(q => q.ModuleId == id)
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();

            var viewModel = new ModuleDetailsViewModel
            {
                Module = module,
                StudyMaterials = studyMaterials,
                Quizzes = quizzes
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Materials()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var applications = await _context.Applications
     .Where(e => e.IdentityUserId == userId
                 && e.Status == Application.ApplicationStatus.Approved
                 && e.PaymentId != null)
     .Include(e => e.Course)
         .ThenInclude(c => c.Modules)
     .ToListAsync();



            if (!applications.Any())
            {
                // The student hasn't completed enrollment yet
                return View("NotEnrolled");
            }

            // Extract all modules
            var modules = applications
                .SelectMany(app => app.Course.Modules)
                .ToList();

            return View(modules); // Now you're passing List<Module>
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
        public async Task<IActionResult> ApplyAdmission(
    Application application,
    IFormFile IdentificationDocumentPath,
    IFormFile AcademicRecordsPath,
    IFormFile MotivationLetterPath)
        {
            // Set up the local folder path
            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "applications");
            Directory.CreateDirectory(uploadsRoot); // Creates if not exists

            // Helper to save file and return relative path
            async Task<string> SaveFileAsync(IFormFile file)
            {
                if (file == null || file.Length == 0)
                    return null;

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsRoot, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the relative path for storage in DB
                return $"/applications/{uniqueFileName}";
            }

            if (IdentificationDocumentPath != null)
                application.IdentificationDocumentPath = await SaveFileAsync(IdentificationDocumentPath);

            if (AcademicRecordsPath != null)
                application.AcademicRecordsPath = await SaveFileAsync(AcademicRecordsPath);

            if (MotivationLetterPath != null)
                application.MotivationLetterPath = await SaveFileAsync(MotivationLetterPath);

            application.ApplicationDate = DateTime.Now;
            application.Status = Application.ApplicationStatus.Pending;

            var course = _context.Courses
                .Include(c => c.Modules)
                .FirstOrDefault(c => c.Id == application.CourseId);

            application.ApplicationFee = 4500; 
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

        // GET: Student/StartQuiz/5
        public async Task<IActionResult> StartQuiz(int quizId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var quiz = await _context.Quizzes
                .Include(q => q.Module)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null) return NotFound();

            var attempts = _context.StudentQuizAttempts
                .Where(a => a.QuizId == quiz.QuizId && a.StudentId == userId)
                .ToList();

            // 1. If there is an unsubmitted attempt, let the student resume it
            var unsubmittedAttempt = attempts.FirstOrDefault(a => !a.IsSubmitted);
            if (unsubmittedAttempt != null)
            {
                return RedirectToAction("TakeQuiz", new { attemptId = unsubmittedAttempt.AttemptId });
            }

            // 2. If unlimited attempts or not reached max, allow starting new attempt
            if (quiz.MaxAttempts == 0 || attempts.Count < quiz.MaxAttempts)
            {
                return View(quiz);
            }

            // 3. Otherwise, max attempts reached and all are submitted: show results
            return RedirectToAction("QuizResults", new { quizId = quiz.QuizId });
        }


        // GET: Student/TakeQuiz/5
        public async Task<IActionResult> TakeQuiz(int attemptId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var attempt = await _context.StudentQuizAttempts
                .Include(a => a.Quiz).ThenInclude(q => q.Questions)
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == userId);

            if (attempt == null) return NotFound();

            // Check time limit, etc.

            return View(attempt);
        }


        // POST: Student/CreateAttempt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAttempt(int quizId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null) return NotFound();

            var attempt = new StudentQuizAttempt
            {
                QuizId = quizId,
                StudentId = userId,
                StartTime = DateTime.UtcNow,
                IsSubmitted = false
            };
            _context.StudentQuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            // Pre-create blank answers
            foreach (var question in quiz.Questions)
            {
                _context.StudentAnswers.Add(new StudentAnswer
                {
                    AttemptId = attempt.AttemptId,
                    QuestionId = question.QuestionId,
                    Answer = string.Empty
                });
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("TakeQuiz", new { attemptId = attempt.AttemptId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int attemptId, Dictionary<string, string> answers)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var attempt = await _context.StudentQuizAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == userId);

            if (attempt == null) return NotFound();

            // Update answers
            foreach (var ans in attempt.Answers)
            {
                if (answers.TryGetValue($"answer_{ans.QuestionId}", out var value))
                    ans.Answer = value;
            }
            attempt.IsSubmitted = true;
            attempt.SubmissionTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("QuizResults", new { attemptId });
        }

        public async Task<IActionResult> TrackProgress()
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all attempts with related data
            var attempts = await _context.StudentQuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Module)
                .Include(a => a.Answers)
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            // Group attempts by quiz
            var quizGroups = attempts
                .GroupBy(a => a.Quiz.QuizId)
                .Select(g => new QuizAttemptsViewModel
                {
                    Quiz = g.First().Quiz,
                    Attempts = g.ToList()
                })
                .ToList();

            // Calculate overall statistics
            var statistics = new Dictionary<string, object>
    {
        { "TotalQuizzes", quizGroups.Count },
        { "TotalAttempts", attempts.Count },
        { "CompletedAttempts", attempts.Count(a => a.IsSubmitted) },
        { "AverageScore", attempts.Where(a => a.IsSubmitted && a.Score.HasValue).Select(a => a.Score.Value).DefaultIfEmpty(0).Average() },
        { "BestOverallScore", attempts.Where(a => a.IsSubmitted && a.Score.HasValue).Select(a => a.Score.Value).DefaultIfEmpty(0).Max() }
    };

            ViewBag.Statistics = statistics;

            // Set ViewBag values with the exact requested format
            ViewBag.CurrentDateTime = "2025-05-16 11:06:58";
            ViewBag.CurrentUser = "NiqueWrld";

            return View(quizGroups);
        }

        // Action to generate PDF report
        public async Task<IActionResult> DownloadProgressReport()
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user details
            var user = await _userManager.FindByIdAsync(userId);

            // Get all attempts with related data
            var attempts = await _context.StudentQuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Module)
                        .ThenInclude(m => m.Course)
                .Include(a => a.Answers)
                .Where(a => a.StudentId == userId && a.IsSubmitted)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            // Create the report data
            var reportData = new
            {
                UserName = user.UserName,
                GeneratedDate = "2025-05-16 11:06:58",
                TotalAttempts = attempts.Count,
                CompletedAttempts = attempts.Count(a => a.IsSubmitted),
                AverageScore = attempts.Where(a => a.Score.HasValue)
                                      .Select(a => a.Score.Value)
                                      .DefaultIfEmpty(0)
                                      .Average(),
                ModuleDetails = attempts
                    .GroupBy(a => a.Quiz.Module.ModuleId)
                    .Select(g => new
                    {
                        ModuleName = g.First().Quiz.Module.ModuleName,
                        ModuleCode = g.First().Quiz.Module.ModuleCode,
                        CourseName = g.First().Quiz.Module.Course.CourseName,
                        Quizzes = g.GroupBy(a => a.Quiz.QuizId)
                                   .Select(qg => new
                                   {
                                       QuizTitle = qg.First().Quiz.Title,
                                       AttemptCount = qg.Count(),
                                       BestScore = qg.Where(a => a.Score.HasValue)
                                                   .Select(a => a.Score.Value)
                                                   .DefaultIfEmpty(0)
                                                   .Max(),
                                       LastAttemptDate = qg.Max(a => a.StartTime).ToString("yyyy-MM-dd")
                                   })
                                   .OrderBy(q => q.QuizTitle)
                                   .ToList()
                    })
                    .OrderBy(m => m.ModuleName)
                    .ToList()
            };

            // Pass the data to the client-side for PDF generation
            return Json(reportData);
        }

        // GET: Student/QuizResults/5
        public async Task<IActionResult> QuizResults(int attemptId)
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get attempt with all related data
            var attempt = await _context.StudentQuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Module)
                .Include(a => a.Quiz.Questions)
                .Include(a => a.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == userId);

            if (attempt == null)
            {
                TempData["ErrorMessage"] = "Quiz attempt not found.";
                return RedirectToAction("Materials");
            }

            // If not submitted yet, redirect to take quiz
            if (!attempt.IsSubmitted)
            {
                return RedirectToAction("TakeQuiz", new { attemptId = attempt.AttemptId });
            }

            // Calculate statistics
            var totalQuestions = attempt.Quiz.Questions.Count;
            var answeredQuestions = attempt.Answers.Count(a => !string.IsNullOrWhiteSpace(a.Answer));
            var correctAnswers = attempt.Answers.Count(a => a.Answer == a.Question.CorrectAnswer);
            var incorrectAnswers = attempt.Answers.Count(a => a.Answer != a.Question.CorrectAnswer);
            var pendingReview = attempt.Answers.Count(a => a.IsCorrect == null && !string.IsNullOrWhiteSpace(a.Answer));
            var totalPoints = attempt.Quiz.Questions.Sum(q => q.Points);
            var earnedPoints = attempt.Answers
    .Where(a => a.Answer == a.Question.CorrectAnswer)
    .Sum(a => a.Question.Points);

            var scorePercentage = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0;

            ViewBag.Statistics = new Dictionary<string, object>
    {
        { "TotalQuestions", totalQuestions },
        { "AnsweredQuestions", answeredQuestions },
        { "CorrectAnswers", correctAnswers },
        { "IncorrectAnswers", incorrectAnswers },
        { "PendingReview", pendingReview },
        { "TotalPoints", totalPoints },
        { "EarnedPoints", earnedPoints },
        { "ScorePercentage", scorePercentage }
    };

            // Get previous attempts for comparison
            var previousAttempts = await _context.StudentQuizAttempts
                .Where(a => a.QuizId == attempt.QuizId && a.StudentId == userId && a.IsSubmitted && a.AttemptId != attemptId)
                .OrderByDescending(a => a.SubmissionTime)
                .Select(a => new
                {
                    a.AttemptId,
                    a.StartTime,
                    a.SubmissionTime,
                    a.Score
                })
                .ToListAsync();

            ViewBag.PreviousAttempts = previousAttempts;

            // Set ViewBag values for display
            ViewBag.CurrentDateTime = "2025-05-15 12:52:14";
            ViewBag.CurrentUser = "NiqueWrld";

            return View(attempt);
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