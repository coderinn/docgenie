using Microsoft.AspNetCore.Mvc;

namespace LeadsPortal.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(
            string businessName,
            string email,
            string password,
            bool rememberMe = false)
        {
            // Temporary test only.
            // Real authentication will be implemented next.

            return RedirectToAction("Index", "Home");
        }
    }
}