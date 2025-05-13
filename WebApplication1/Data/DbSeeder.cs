using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = serviceProvider.GetRequiredService<NexelContext>();

            string[] roleNames = { "Admin", "Student", "Lecturer" };

            // Seed roles
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed admin user
            string adminEmail = "admin@nexel.com";
            string adminPassword = "Admin@123";

            var existingAdminUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdminUser == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var adminResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (adminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed student user
            string studentEmail = "student@nexel.com";
            string studentPassword = "Student@123";

            var existingStudentUser = await userManager.FindByEmailAsync(studentEmail);
            if (existingStudentUser == null)
            {
                var studentUser = new IdentityUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true
                };

                var studentResult = await userManager.CreateAsync(studentUser, studentPassword);
                if (studentResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");
                }
            }

            // Seed lecturer user
            string lecturerEmail = "lecturer@nexel.com";
            string lecturerPassword = "Lecture@123";

            var existingLecturerUser = await userManager.FindByEmailAsync(lecturerEmail);
            if (existingLecturerUser == null)
            {
                var lecturerUser = new IdentityUser
                {
                    UserName = lecturerEmail,
                    Email = lecturerEmail,
                    EmailConfirmed = true
                };

                var lecturerResult = await userManager.CreateAsync(lecturerUser, lecturerPassword);
                if (lecturerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(lecturerUser, "Lecturer");
                }
            }


            // Seed courses and modules if they don't exist
            if (!dbContext.Courses.Any())
            {
                var course1 = new Course
                {
                    Faculty = "Engineering",
                    CourseName = "Computer Science",
                    CourseCode = "CS101",
                    Description = "Introduction to Computer Science",
                    DurationYears = 4,
                    Modules = new List<Module>
                    {
                        new Module { ModuleName = "Programming 101", ModuleCode = "CS101A", Description = "Basic Programming Concepts" , Year = "1" },
                        new Module { ModuleName = "Data Structures", ModuleCode = "CS102A", Description = "Introduction to Data Structures", Year = "1" }
                    }
                };

                dbContext.Courses.Add(course1);
                await dbContext.SaveChangesAsync();
            }

        }
    }
}
