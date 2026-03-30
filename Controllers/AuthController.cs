using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

/// <summary>
/// Controlador para autenticación y gestión de sesión de usuario.
/// </summary>
public class AuthController : BaseController
{
    /// <summary>
    /// Constructor que recibe los servicios de API y autenticación.
    /// </summary>
    public AuthController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    /// <summary>
    /// Muestra la vista de login. Si el usuario ya está autenticado, redirige según su rol.
    /// </summary>
    /// <returns>Vista de login o redirección.</returns>
    [HttpGet]
    public IActionResult Login()
    {
        if (Auth.IsAuthenticated())
            return RedirectByRole();

        return View(new LoginViewModel());
    }

    /// <summary>
    /// Procesa el formulario de login y autentica al usuario.
    /// </summary>
    /// <param name="model">Modelo con email y contraseña.</param>
    /// <returns>Redirección según rol o vista con error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.PostAsync<TokenRespuestaViewModel>("/api/auth/login", new
        {
            email = model.Email,
            contrasena = model.Contrasena
        });

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Credenciales incorrectas.");
            return View(model);
        }

        Auth.StoreToken(result.Data!.Token, result.Data.Email, result.Data.Rol);
        return RedirectByRole();
    }

    /// <summary>
    /// Cierra la sesión del usuario y redirige al login.
    /// </summary>
    /// <returns>Redirección a la vista de login.</returns>
    [HttpGet]
    public IActionResult Logout()
    {
        Auth.Clear();
        return RedirectToAction("Login");
    }

    // Método privado para redirigir según el rol del usuario autenticado
    private IActionResult RedirectByRole() => Auth.GetUserRole() switch
    {
        "Administrador" => RedirectToAction("Dashboard", "Admin"),
        "Doctor"        => RedirectToAction("Dashboard", "Medico"),
        "Paciente"      => RedirectToAction("Dashboard", "Paciente"),
        _               => RedirectToAction("Login")
    };
}
