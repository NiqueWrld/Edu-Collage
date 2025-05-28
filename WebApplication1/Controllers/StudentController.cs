using Braintree;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

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
        private NotificationService _notificationService;
        TimeZoneInfo southAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

        public StudentController(NexelContext context, UserManager<IdentityUser> userManager, IConfiguration configuration, BraintreeGateway braintreeGateway, NotificationService notificationService,  ILogger<StudentController> logger = null )
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _braintreeGateway = braintreeGateway;
            _notificationService = notificationService;
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
                    PaymentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone),
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

        [HttpGet]
        public async Task<IActionResult> PayModules()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get approved application and related modules
            var application = await _context.Applications
                .Where(a => a.IdentityUserId == userId && a.Status == Application.ApplicationStatus.Approved && a.PaymentId != null)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Modules)
                .FirstOrDefaultAsync();

            if (application == null)
                return RedirectToAction("Dashboard");

            // Get unpaid modules
            var paidModuleIds = await _context.ModulePayments
                .Where(mp => mp.IdentityUserId == userId && mp.Status == ModulePaymentStatus.Completed)
                .Select(mp => mp.ModuleId)
                .ToListAsync();

            var unpaidModules = application.Course.Modules
                .Where(m => !paidModuleIds.Contains(m.ModuleId))
                .Select(m => new ModulePaymentSelectionViewModel
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName,
                    ModuleCode = m.ModuleCode,
                    ModulePrice = m.ModulePrice,
                    Year = m.Year,
                    Semester = m.Semester.ToString(),
                    Selected = false
                }).ToList();

            var viewModel = new ModulePaymentListViewModel
            {
                Modules = unpaidModules,
                Total = 0
            };

            var clientToken = await _braintreeGateway.ClientToken.GenerateAsync();
            ViewData["ClientToken"] = clientToken;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayModules(ModulePaymentListViewModel model, string paymentNonce)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var selectedModules = model.Modules.Where(m => m.Selected).ToList();
            if (!selectedModules.Any())
            {
                ModelState.AddModelError("", "Please select at least one module to pay for.");
                return View(model);
            }

            var total = selectedModules.Sum(m => m.ModulePrice);

            // Payment logic (Braintree, etc.)
            var request = new TransactionRequest
            {
                Amount = total,
                PaymentMethodNonce = paymentNonce,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };

            var result = await _braintreeGateway.Transaction.SaleAsync(request);

            if (result.IsSuccess())
            {
                foreach (var module in selectedModules)
                {
                    var payment = new ModulePayment
                    {
                        IdentityUserId = userId,
                        ModuleId = module.ModuleId,
                        Amount = module.ModulePrice,
                        PaymentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone),
                        Status = ModulePaymentStatus.Completed
                    };
                    _context.ModulePayments.Add(payment);
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment completed successfully!";
                return RedirectToAction("PayModules");
            }
            else
            {
                TempData["ErrorMessage"] = $"Payment failed: {result.Message}";
                return View(model);
            }
        }

        public class ModulePaymentSelectionViewModel
        {
            public int ModuleId { get; set; }
            public string ModuleName { get; set; }
            public string ModuleCode { get; set; }
            public string Year { get; set; }
            public string Semester { get; set; }
            public decimal ModulePrice { get; set; }
            public bool Selected { get; set; }
        }

        public class ModulePaymentListViewModel
        {
            public List<ModulePaymentSelectionViewModel> Modules { get; set; }
            public decimal Total { get; set; }
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

            bool hasPaid = await _context.ModulePayments
        .AnyAsync(mp => mp.ModuleId == id && mp.IdentityUserId == userId && mp.Status == ModulePaymentStatus.Completed);

            if (!hasPaid)
            {
                TempData["ErrorMessage"] = "You must pay for this module to access its content.";
                return RedirectToAction("PayModules");
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

            var assignments = await _context.Assignments
              .Where(q => q.ModuleId == id)
              .OrderByDescending(q => q.DueDate)
              .ToListAsync();

            var submissions = await _context.AssignmentSubmissions
             .Where(q => q.StudentId == userId)
             .ToListAsync();

            var viewModel = new ModuleDetailsViewModel
            {
                Module = module,
                StudyMaterials = studyMaterials,
                Quizzes = quizzes,
                Assignments = assignments,
                Submissions = submissions
            };

            return View(viewModel);
        }

        public IActionResult SubmitAssignment(int assignmentId)
        {
            // Get assignment details
            var assignment = _context.Assignments
                .Include(a => a.Module)
                .FirstOrDefault(a => a.AssignmentId == assignmentId);

            if (assignment == null)
                return NotFound();

            // Get previous submission if exists
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var previousSubmission = _context.AssignmentSubmissions
                .FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentId == userId);

            var viewModel = new AssignmentSubmissionViewModel
            {
                AssignmentId = assignmentId,
                Assignment = assignment,
                ModuleName = assignment.Module.ModuleName,
                PreviousSubmission = previousSubmission
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAssignment(AssignmentSubmissionViewModel model, IFormFile submissionFile)
        {
            if (submissionFile == null)
            {
                ModelState.AddModelError("SubmissionFile", "Please select a file to upload");
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var assignment = await _context.Assignments.FindAsync(model.AssignmentId);

            if (assignment == null)
                return NotFound();

            // Save file
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = userId + "_" + Guid.NewGuid().ToString() + "_" + submissionFile.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await submissionFile.CopyToAsync(fileStream);
            }

            // Create or update submission
            var existingSubmission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == model.AssignmentId && s.StudentId == userId);

            if (existingSubmission != null)
            {
                // Delete old file if it exists
                if (!string.IsNullOrEmpty(existingSubmission.FileUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), existingSubmission.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                existingSubmission.FileUrl = "/submissions/" + uniqueFileName;
                existingSubmission.Comments = model.Comments;
                existingSubmission.SubmissionDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);

                _context.Update(existingSubmission);
            }
            else
            {
                var submission = new AssignmentSubmission
                {
                    AssignmentId = model.AssignmentId,
                    StudentId = userId,
                    FileUrl = "/uploads/submissions/" + uniqueFileName,
                    Comments = model.Comments,
                    SubmissionDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone)
                };

                _context.AssignmentSubmissions.Add(submission);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assignment submitted successfully.";
            return RedirectToAction("ModuleDetails", new { id = assignment.ModuleId });
        }

        public async Task<IActionResult> Materials()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var application = await _context.Applications
    .Where(e => e.IdentityUserId == userId
                && e.Status == Application.ApplicationStatus.Approved
                && e.PaymentId != null)
    .Include(e => e.Course)
        .ThenInclude(c => c.Modules)
    .FirstOrDefaultAsync();


            if (application == null)
            {
                // The student hasn't completed enrollment yet
                return View("NotEnrolled");
            }

            // Extract all modules
            var modules = application?.Course?.Modules?.ToList() ?? new List<Module>();

            ViewBag.EnrolledYear = application.ApprovedDate;

            return View(modules); // Now you're passing List<Module>
        }


        [HttpGet]
        public async Task<IActionResult> ApplyAdmission()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.IdentityUserId == userId);

            if (application != null)
            {
                return RedirectToAction(nameof(TrackApplications));
            }

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
            // Example logic in StartQuiz action
            string cannotStartReason = null;
            if (quiz.StartDate != null && quiz.StartDate > DateTime.Now)
            {
                cannotStartReason = "Quiz is not open yet.";
            }
            else if (quiz.EndDate != null && quiz.EndDate < DateTime.Now)
            {
                cannotStartReason = "Quiz has already closed.";
            }
            else if (quiz.MaxAttempts > 0 && attempts.Count >= quiz.MaxAttempts)
            {
                cannotStartReason = "Max attempts reached.";
            }

            ViewBag.CanStartQuiz = string.IsNullOrEmpty(cannotStartReason);
            ViewBag.CannotStartReason = cannotStartReason;



            return View(quiz);

        }

        // GET: Student/StudentProgress?moduleId=4
        public async Task<IActionResult> StudentProgress(int moduleId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all quizzes for the module
            var quizzes = await _context.Quizzes
                .Where(q => q.ModuleId == moduleId && q.IsPublished)
                .Include(q => q.Questions)
                .ToListAsync();

            // Get all attempts by this student for these quizzes
            var attempts = await _context.StudentQuizAttempts
                .Where(a => a.StudentId == userId && quizzes.Select(q => q.QuizId).Contains(a.QuizId))
                .ToListAsync();

            // Build a view model
            // Build a view model
            var quizProgress = quizzes.Select(q =>
            {
                // Find the attempt with the highest score for this quiz
                var bestAttempt = attempts
                    .Where(a => a.QuizId == q.QuizId && a.SubmissionTime.HasValue)
                    .OrderByDescending(a => a.Score ?? 0)
                    .ThenByDescending(a => a.SubmissionTime)
                    .FirstOrDefault();

                int totalPoints = q.Questions.Sum(qq => qq.Points);
                int earnedPoints = 0;
                decimal percentage = 0;

                if (bestAttempt != null)
                {
                    // Load answers for this attempt and their related questions
                    var attemptAnswers = _context.StudentAnswers
                        .Where(a => a.AttemptId == bestAttempt.AttemptId)
                        .Include(a => a.Question)
                        .ToList();

                    earnedPoints = attemptAnswers
                        .Where(a => a.IsCorrect == true)
                        .Sum(a => a.PointsAwarded ?? 0);

                    percentage = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0;
                }

                return new WebApplication1.Models.ViewModels.StudentQuizProgressViewModel
                {
                    QuizId = q.QuizId,
                    QuizTitle = q.Title,
                    AttemptId = bestAttempt?.AttemptId,
                    AttemptDate = bestAttempt?.SubmissionTime,
                    Score = earnedPoints,
                    TotalPoints = totalPoints,
                    Percentage = percentage,
                    Status = bestAttempt == null ? "Not Attempted" : "Completed"
                };
            }).ToList();


            var module = await _context.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.ModuleId == moduleId);

            var viewModel = new WebApplication1.Models.ViewModels.StudentProgressViewModel2
            {
                Module = module,
                QuizProgress = quizProgress
            };

            return View(viewModel);
        }



        [Authorize]
        public async Task<IActionResult> Calendar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get approved, paid applications for this student
            var applications = await _context.Applications
                .Where(a => a.IdentityUserId == userId && a.Status == Application.ApplicationStatus.Approved && a.PaymentId != null)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Modules)
                .ToListAsync();

            // Get all modules the student is enrolled in
            var modules = applications.SelectMany(a => a.Course.Modules).ToList();

            // Get quizzes for these modules
            var moduleIds = modules.Select(m => m.ModuleId).ToList();
            var quizzes = await _context.Quizzes
                .Where(q => moduleIds.Contains(q.ModuleId))
                .ToListAsync();

            var assignments = await _context.Assignments
        .Where(a => moduleIds.Contains(a.ModuleId))
        .ToListAsync();

            var assignmentSubmissions = await _context.AssignmentSubmissions
       .Where(s => s.StudentId == userId)
       .ToListAsync();

            // Pass both to the view
            var viewModel = new StudentCalendarViewModel
            {
                Modules = modules,
                Quizzes = quizzes,
                Assignments = assignments,
                AssignmentSubmissions = assignmentSubmissions
            };

            return View(viewModel);
        }

        // Add this action to your StudentController
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.MarkNotificationAsReadAsync(id, userId);
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return RedirectToAction(nameof(Notifications));
        }

        public class StudentCalendarViewModel
        {
            public List<Module> Modules { get; set; }
            public List<Quiz> Quizzes { get; set; }
            public List<Assignment> Assignments { get; set; }
            public List<AssignmentSubmission> AssignmentSubmissions { get; set; }
        }

        // GET: Student/ViewSubmission?assignmentId=6
        public async Task<IActionResult> ViewSubmission(int assignmentId)
        {
            // Get the current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get the assignment
            var assignment = await _context.Assignments
                .Include(a => a.Module)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null)
                return NotFound();

            // Get the student's submission for this assignment
            var submission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == userId);

            if (submission == null)
            {
                TempData["ErrorMessage"] = "You haven't submitted this assignment yet.";
                return RedirectToAction("ModuleDetails", new { id = assignment.ModuleId });
            }

            // Create the view model
            var viewModel = new AssignmentSubmissionViewModel
            {
                Assignment = assignment,
                ModuleName = assignment.Module.ModuleName,
                FileUrl = submission.FileUrl,
                Comments = submission.Comments,
                SubmissionDate = submission.SubmissionDate,
                FeedbackFromLecturer = submission.FeedbackFromLecturer,
                Grade = submission.Grade
            };

            return View(viewModel);
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
                StartTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone),
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
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Questions)
                .Include(a => a.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == userId);

            if (attempt == null) return NotFound();

            // Update answers
            foreach (var ans in attempt.Answers)
            {
                if (answers.TryGetValue($"answer_{ans.QuestionId}", out var value))
                    ans.Answer = value;

                if ((ans.Question.Type == QuestionType.MultipleChoice || ans.Question.Type == QuestionType.TrueFalse)
                    && ans.Answer == ans.Question.CorrectAnswer)
                {
                    ans.IsCorrect = true;
                    ans.PointsAwarded = ans.Question.Points;
                }
                else
                {
                    ans.IsCorrect = false;
                    ans.PointsAwarded = 0;
                }
            }

            attempt.IsSubmitted = true;

            attempt.SubmissionTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZone);

            // Check if the quiz contains any short answer questions
            bool hasShortQuestion = attempt.Quiz.Questions.Any(q => q.Type == QuestionType.ShortAnswer);

            if (!hasShortQuestion)
            {
                // Calculate and save the score
                attempt.Score = attempt.Answers
                    .Where(a => a.IsCorrect == true)
                    .Sum(a => a.PointsAwarded ?? 0);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("QuizResults", new { attemptId });
        }


        public async Task<IActionResult> TrackProgress()
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all quiz attempts with related data (existing code)
            var attempts = await _context.StudentQuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Module)
                .Include(a => a.Answers)
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            // Group attempts by quiz (existing code)
            var quizGroups = attempts
                .GroupBy(a => a.Quiz.QuizId)
                .Select(g => new QuizAttemptsViewModel
                {
                    Quiz = g.First().Quiz,
                    Attempts = g.ToList()
                })
                .ToList();

            // Get assignment submissions for the current user
            var assignmentSubmissions = await _context.AssignmentSubmissions
                .Include(a => a.Assignment)
                    .ThenInclude(a => a.Module)
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            // Calculate combined statistics
            var statistics = new Dictionary<string, object>
    {
        { "TotalQuizzes", quizGroups.Count },
        { "TotalAttempts", attempts.Count },
        { "CompletedAttempts", attempts.Count(a => a.IsSubmitted) },
        { "AverageScore", attempts.Where(a => a.IsSubmitted && a.Score.HasValue).Select(a => a.Score.Value).DefaultIfEmpty(0).Average() },
        { "BestOverallScore", attempts.Where(a => a.IsSubmitted && a.Score.HasValue).Select(a => a.Score.Value).DefaultIfEmpty(0).Max() },
        // Assignment statistics
        { "TotalAssignments", assignmentSubmissions.Select(a => a.AssignmentId).Distinct().Count() },
        { "SubmittedAssignments", assignmentSubmissions.Count },
        { "GradedAssignments", assignmentSubmissions.Count(a => a.Grade.HasValue) },
        { "AverageAssignmentGrade", assignmentSubmissions.Where(a => a.Grade.HasValue).Select(a => a.Grade.Value).DefaultIfEmpty(0).Average() }
    };

            ViewBag.Statistics = statistics;
            ViewBag.AssignmentSubmissions = assignmentSubmissions;
            ViewBag.CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ViewBag.CurrentUser = User.Identity.Name;

            return View(quizGroups);
        }

        // Action to generate PDF report
        public async Task<IActionResult> DownloadProgressReport()
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user details
            var user = await _userManager.FindByIdAsync(userId);

            // Get all attempts with related data (existing code)
            var attempts = await _context.StudentQuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Module)
                        .ThenInclude(m => m.Course)
                .Include(a => a.Answers)
                .Where(a => a.StudentId == userId && a.IsSubmitted)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            // Get assignment submissions
            var assignmentSubmissions = await _context.AssignmentSubmissions
                .Include(a => a.Assignment)
                    .ThenInclude(a => a.Module)
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            // Create the report data
            var reportData = new
            {
                UserName = user.UserName,
                UserFullName = user.UserName, // Replace with actual name property if available
                GeneratedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
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
                    .ToList(),
                // Add assignment submission data
                AssignmentSubmissions = assignmentSubmissions
                    .Select(a => new
                    {
                        Title = a.Assignment.Title,
                        ModuleName = a.Assignment.Module.ModuleName,
                        ModuleCode = a.Assignment.Module.ModuleCode,
                        SubmissionDate = a.SubmissionDate.ToString("yyyy-MM-dd"),
                        Grade = a.Grade,
                        HasFeedback = !string.IsNullOrEmpty(a.FeedbackFromLecturer)
                    })
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
                return RedirectToAction("ModuleDetails");
            }

            if (!attempt.IsSubmitted)
            {
                return RedirectToAction("TakeQuiz", new { attemptId = attempt.AttemptId });
            }

            // Calculate statistics
            var totalQuestions = attempt.Quiz.Questions.Count;
            var answeredQuestions = attempt.Answers.Count(a => !string.IsNullOrWhiteSpace(a.Answer));
            var correctAnswers = attempt.Answers.Count(a => a.IsCorrect == true);
            var incorrectAnswers = attempt.Answers.Count(a => a.IsCorrect == false);
            var pendingReview = attempt.Answers.Count(a => a.IsCorrect == null && !string.IsNullOrWhiteSpace(a.Answer));
            var totalPoints = attempt.Quiz.Questions.Sum(q => q.Points);
            var earnedPoints = attempt.Answers
                .Where(a => a.IsCorrect == true)
                .Sum(a => a.PointsAwarded);

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