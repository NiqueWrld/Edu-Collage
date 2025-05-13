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
    .OnDelete(DeleteBehavior.Restrict); // or DeleteBehavior.NoAction (EF Core 5+)

    }
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<Performance> Performances { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<LibraryBooking> LibraryBookings { get; set; }
}
