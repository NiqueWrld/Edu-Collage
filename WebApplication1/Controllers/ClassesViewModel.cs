using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class ClassesViewModel
    {
        public string StudentName { get; set; }
        public Course Course { get; set; }
        public int StudyYear { get; set; }
        public List<Module> EnrolledModules { get; set; }
    }
}