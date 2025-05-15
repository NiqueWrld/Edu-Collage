using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    public class ChatController : Controller
    {

        private readonly NexelContext _context;

        public ChatController(NexelContext context)
        {
            _context = context;
        }

        public IActionResult Index(int id)
        {
            var module = _context.Modules.FirstOrDefault(m => m.ModuleId == id);


            if(module == null)
            {
                return NotFound("Module Not Found");
            }

            ViewBag.ModuleId = id;
            ViewBag.ModuleName = module.ModuleName;

            return View();
        }
    }
}
