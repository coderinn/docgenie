using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LeadsPortal.Controllers
{
    [Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
       

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
          
        }
        //https://localhost:7219/pencil.glb
        public IActionResult Threejs()
        {
            return View();
        }

        public IActionResult login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";

            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
