using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LeadsPortal.Models;

namespace LeadsPortal.Controllers
{
    public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string? email, string? password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var result = await signInManager.PasswordSignInAsync(
                email.Trim(), password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Email or password is invalid.");
            return View();
        }
    }
}
