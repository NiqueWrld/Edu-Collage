using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Index(int id)
        {

            if(id == null)
            {
                return NotFound("Module Not Found");
            }

            string name = "NAME";

            ViewBag.ModuleId = id;
            ViewBag.ModuleName = name;

            return View();
        }
    }
}
