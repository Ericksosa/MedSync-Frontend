using System.Diagnostics;
using MedSync_Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedSync_Frontend.Controllers
{
    /// <summary>
    /// Controlador para acciones generales de la aplicación (Home, Error, etc).
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor que recibe el logger para HomeController.
        /// </summary>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Acción principal, redirige al login.
        /// </summary>
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Auth");
        }

        /// <summary>
        /// Muestra la vista de privacidad.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Muestra la vista de error con el identificador de la solicitud.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
