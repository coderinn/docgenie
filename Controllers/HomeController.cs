using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using LeadsPortal.Classes.Base;

using System.Diagnostics;

namespace LeadsPortal.Controllers
{
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
