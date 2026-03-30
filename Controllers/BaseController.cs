using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

/// <summary>
/// Controlador base para los controladores MVC, provee acceso a servicios comunes y utilidades.
/// </summary>
public class BaseController : Controller
{
    /// <summary>
    /// Cliente HTTP para consumir la API.
    /// </summary>
    protected readonly ApiClient Api;
    /// <summary>
    /// Servicio de autenticación y sesión.
    /// </summary>
    protected readonly AuthTokenService Auth;

    /// <summary>
    /// Constructor base que inyecta los servicios comunes.
    /// </summary>
    public BaseController(ApiClient api, AuthTokenService auth)
    {
        Api = api;
        Auth = auth;
    }

    /// <summary>
    /// Redirige a la vista de login.
    /// </summary>
    protected IActionResult RedirectToLogin()
    {
        return RedirectToAction("Login", "Auth");
    }

    /// <summary>
    /// Verifica si el usuario está autenticado.
    /// </summary>
    protected bool RequireAuth()
    {
        return Auth.IsAuthenticated();
    }

    /// <summary>
    /// Verifica si el usuario tiene el rol especificado.
    /// </summary>
    /// <param name="role">Rol requerido.</param>
    protected bool RequireRole(string role)
    {
        return Auth.IsAuthenticated() && Auth.GetUserRole() == role;
    }

    /// <summary>
    /// Almacena un mensaje de error temporal para mostrar en la vista.
    /// </summary>
    protected void SetError(string message) => TempData["Error"] = message;
    /// <summary>
    /// Almacena un mensaje de éxito temporal para mostrar en la vista.
    /// </summary>
    protected void SetSuccess(string message) => TempData["Success"] = message;
}
