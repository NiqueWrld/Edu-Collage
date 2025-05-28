using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class NexelContext : IdentityDbContext<IdentityUser>
{
    public NexelContext(DbContextOptions<NexelContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Student>()
           .HasOne(s => s.IdentityUser)
           .WithMany()
           .HasForeignKey(s => s.IdentityUserId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Application>()
    .HasOne(a => a.Payment)
    .WithMany()
    .HasForeignKey(a => a.PaymentId)
    .OnDelete(DeleteBehavior.Restrict);

        // Configure many-to-many relationship between Module and Lecturer
        builder.Entity<ModuleLecturer>()
            .HasOne(ml => ml.Module)
            .WithMany()
            .HasForeignKey(ml => ml.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ModuleLecturer>()
            .HasOne(ml => ml.Lecturer)
            .WithMany()
            .HasForeignKey(ml => ml.LecturerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Quiz relationships - use NoAction to prevent cascading deletes
        builder.Entity<QuizQuestion>()
            .HasOne(q => q.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Fix the cascade delete issue for StudentQuizAttempts
        builder.Entity<StudentQuizAttempt>()
            .HasOne(sqa => sqa.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(sqa => sqa.QuizId)
            .OnDelete(DeleteBehavior.NoAction); // Using NoAction instead of Cascade

        // Configure StudentAnswer relationships to avoid multiple cascade paths
        builder.Entity<StudentAnswer>()
            .HasOne(sa => sa.Question)
            .WithMany(q => q.StudentAnswers)
            .HasForeignKey(sa => sa.QuestionId)
            .OnDelete(DeleteBehavior.NoAction); // Using NoAction instead of Cascade

        builder.Entity<StudentAnswer>()
            .HasOne(sa => sa.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(sa => sa.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

    }
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
    
    public DbSet<Payment> Payments { get; set; }
    public DbSet<LibraryBooking> LibraryBookings { get; set; }

    public DbSet<LibraryResource> LibraryResources { get; set; }
    public DbSet<ResourceBooking> ResourceBookings { get; set; }

    public DbSet<ModuleLecturer> ModuleLecturers { get; set; }
    public DbSet<StudyMaterial> StudyMaterials { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<ModulePayment> ModulePayments { get; set; }

    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<StudentQuizAttempt> StudentQuizAttempts { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<Notification> Notifications { get; internal set; }
}
