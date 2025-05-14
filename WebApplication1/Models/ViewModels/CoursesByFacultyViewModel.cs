using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class CoursesByFacultyViewModel
    {
        public Dictionary<string, List<Course>> CoursesByFaculty { get; set; } = new Dictionary<string, List<Course>>();
    }
}