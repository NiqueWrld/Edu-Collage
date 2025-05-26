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

            // Seed admin users
            var adminUsers = new List<(string Email, string Password)>
{
    ("admin1@nexel.com", "Admin@123"),
    ("admin2@nexel.com", "Admin@123"),
    ("admin3@nexel.com", "Admin@123"),
    ("admin4@nexel.com", "Admin@123"),
    ("admin5@nexel.com", "Admin@123")
};

            foreach (var (email, password) in adminUsers)
            {
                var existingAdminUser = await userManager.FindByEmailAsync(email);
                if (existingAdminUser == null)
                {
                    var adminUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var adminResult = await userManager.CreateAsync(adminUser, password);
                    if (adminResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    else
                    {
                        // Optional: log or handle failure
                        foreach (var error in adminResult.Errors)
                        {
                            Console.WriteLine($"Error creating {email}: {error.Description}");
                        }
                    }
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
                    Faculty = "Accounting and Informatics",
                    CourseName = "Diploma in ICT: Applications Development",
                    CourseCode = "DIIAD1",
                    Description = "The qualification aims to provide a professional, vocational, or career-focused \r\nqualification for the Information Communication Technology industry (ICT). The \r\nknowledge emphasises general principles and applications. Furthermore, the diploma \r\ndevelops learners who can demonstrate focused knowledge and skills to analyse, \r\ndesign and produce software products and systems to meet specified needs so that \r\nthey work reliably, and their production and maintenance are cost effective. This \r\nspecialisation expands the purpose of the qualification by enabling learners to \r\nconceptualise, design, implement, and test application development solutions to \r\naddress industry-related ICT initiatives",
                    DurationYears = 3,
                    Modules = new List<Module>
{
    // YEAR ONE - SEMESTER 1
    new Module { ModuleName = "Information & Communications Technology Literacy & Skills", ModuleCode = "ICTL101", Description = "Fundamental ICT skills and digital literacy for academic success", Year = "1" , Semester = Module.SemesterNumber.Semester1 , ModulePrice = 3450 },
    new Module { ModuleName = "Fac. Business Fundamentals I", ModuleCode = "BFND101", Description = "Introduction to core business concepts and principles", Year = "1", Semester = Module.SemesterNumber.Semester1 , ModulePrice = 3800 },
    new Module { ModuleName = "Fundamentals of Computer Security", ModuleCode = "FCSC101", Description = "Basic principles of information security and cybersecurity", Year = "1", Semester = Module.SemesterNumber.Semester1 , ModulePrice = 4200 },
    new Module { ModuleName = "Applications Development IA", ModuleCode = "APDA101", Description = "Introduction to programming and software development concepts", Year = "1", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4750 },
    new Module { ModuleName = "Operating Systems", ModuleCode = "OSYS101", Description = "Fundamentals of operating systems architecture and functions", Year = "1", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4100 },
    new Module { ModuleName = "Information Systems 1", ModuleCode = "INSS101", Description = "Introduction to information systems in organizations", Year = "1", Semester = Module.SemesterNumber.Semester1, ModulePrice = 3950 },
    
    // YEAR ONE - SEMESTER 2
    new Module { ModuleName = "Inst. Me, My World, My Universe", ModuleCode = "MWMU101", Description = "Personal development and contextual awareness module", Year = "1", Semester = Module.SemesterNumber.Semester2 , ModulePrice = 3250 },
    new Module { ModuleName = "Inst. Cornerstone 101", ModuleCode = "CSTN101", Description = "Foundation module for higher education success", Year = "1", Semester = Module.SemesterNumber.Semester2 , ModulePrice = 3500 },
    new Module { ModuleName = "Applications Development Project I", ModuleCode = "APDP101", Description = "First practical project in applications development", Year = "1", Semester = Module.SemesterNumber.Semester2 , ModulePrice = 4650 },
    new Module { ModuleName = "Applications Development IB", ModuleCode = "APDB101", Description = "Continuation of programming fundamentals and techniques", Year = "1", Semester = Module.SemesterNumber.Semester2 , ModulePrice = 4700 },
    new Module { ModuleName = "Communications Networks 1", ModuleCode = "CNTW101", Description = "Introduction to computer networking concepts", Year = "1", Semester = Module.SemesterNumber.Semester2 , ModulePrice = 4250 },
    
    // YEAR TWO - SEMESTER 1
    new Module { ModuleName = "Business Fundamentals II", ModuleCode = "BFND201", Description = "Advanced business concepts and organizational management", Year = "2", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4100 },
    new Module { ModuleName = "Mobile Computing IIA", ModuleCode = "MCPA201", Description = "Introduction to mobile application development", Year = "2", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4900 },
    new Module { ModuleName = "Information Systems IIA", ModuleCode = "ISYA201", Description = "Advanced information systems analysis and design", Year = "2", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4350 },
    new Module { ModuleName = "Applications Development IIA", ModuleCode = "APDA201", Description = "Intermediate programming and software development", Year = "2", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4850 },
    new Module { ModuleName = "IT Project Management", ModuleCode = "ITPM101", Description = "Principles and practices of managing IT projects", Year = "2", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4400 },
    new Module { ModuleName = "Information Management 11A", ModuleCode = "INMA201", Description = "Database design and information management concepts", Year = "2", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4300 },
    
    // YEAR TWO - SEMESTER 2
    new Module { ModuleName = "Community Engagement Project", ModuleCode = "CMEP101", Description = "Service learning and community-based project", Year = "2", Semester = Module.SemesterNumber.Semester2, ModulePrice = 3700 },
    new Module { ModuleName = "Mobile Computing IIB", ModuleCode = "MCPB201", Description = "Advanced mobile application development and deployment", Year = "2", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4950 },
    new Module { ModuleName = "Information Systems IIB", ModuleCode = "ISYB201", Description = "Implementation and evaluation of information systems", Year = "2", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4300 },
    new Module { ModuleName = "Applications Development IIB", ModuleCode = "APDB201", Description = "Advanced programming techniques and frameworks", Year = "2", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4800 },
    new Module { ModuleName = "Information Management IIB", ModuleCode = "INMB201", Description = "Advanced database systems and data management", Year = "2", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4250 },
    new Module { ModuleName = "Applications Development Project II", ModuleCode = "APDP201", Description = "Intermediate team-based software development project", Year = "2", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4750 },
    
    // YEAR THREE - SEMESTER 1
    new Module { ModuleName = "Applications Development IIIA", ModuleCode = "APDA301", Description = "Advanced software development methodologies", Year = "3", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4850 },
    new Module { ModuleName = "Information Systems IIIA", ModuleCode = "ISYA301", Description = "Strategic information systems planning and implementation", Year = "3", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4600 },
    new Module { ModuleName = "Applications Development Project IIIA", ModuleCode = "ADPA301", Description = "Advanced development project with industry requirements", Year = "3", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4900 },
    new Module { ModuleName = "Human Computer Interaction", ModuleCode = "HCIN101", Description = "Principles of user experience and interface design", Year = "3", Semester = Module.SemesterNumber.Semester1, ModulePrice = 4400 },
    new Module { ModuleName = "Theory of ICT Professional Practice III", ModuleCode = "TIPP301", Description = "Professional ethics and practices in the ICT field", Year = "3", Semester = Module.SemesterNumber.Semester1, ModulePrice = 3800 },
    
    // YEAR THREE - SEMESTER 2
    new Module { ModuleName = "Entrepreneurial Spirit", ModuleCode = "ENSP101", Description = "Entrepreneurship and innovation in technology", Year = "3", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4000 },
    new Module { ModuleName = "Applications Development IIIB", ModuleCode = "APDB301", Description = "Specialized and emerging software development topics", Year = "3", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4850 },
    new Module { ModuleName = "Information Systems IIIB", ModuleCode = "ISYB301", Description = "Integration of enterprise information systems", Year = "3", Semester = Module.SemesterNumber.Semester2, ModulePrice = 4550 },
    new Module { ModuleName = "Applications Development Project IIIB", ModuleCode = "ADPB301", Description = "Capstone project in applications development", Year = "3", Semester = Module.SemesterNumber.Semester2, ModulePrice = 5000 }
}
                };

                dbContext.Courses.Add(course1);
                await dbContext.SaveChangesAsync();
            }

        }
    }
}
