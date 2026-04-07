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
    [HttpGet]
    public IActionResult Logout()
    {
        Auth.Clear();
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Muestra el formulario de auto-registro para pacientes.
    /// </summary>
    [HttpGet]
    public IActionResult Registro()
    {
        if (Auth.IsAuthenticated()) return RedirectByRole();
        return View(new PacienteRegistroViewModel());
    }

    /// <summary>
    /// Procesa el registro de un nuevo paciente.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(PacienteRegistroViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Registro unificado: cuenta + perfil de paciente
        var authResult = await Api.PostAsync<TokenRespuestaViewModel>("/api/auth/registro-paciente", new
        {
            email = model.Email,
            contrasena = model.Contrasena,
            nombre = model.Nombre,
            apellido = model.Apellido,
            rol = "Paciente",
            documentoId = model.DocumentoId,
            telefono = model.Telefono,
            fechaNacimiento = model.FechaNacimiento
        });

        if (!authResult.Success)
        {
            ModelState.AddModelError("", authResult.Error ?? "Error al crear la cuenta. El email puede estar en uso.");
            return View(model);
        }

        Auth.StoreToken(authResult.Data!.Token, authResult.Data.Email, authResult.Data.Rol);

        TempData["Success"] = "Cuenta creada exitosamente. Bienvenido a MedSync.";
        return RedirectToAction("Dashboard", "Paciente");
    }

    /// <summary>
    /// Muestra la pantalla de recuperación de contraseña.
    /// </summary>
    [HttpGet]
    public IActionResult RecuperarContrasena()
    {
        return View();
    }

    // Método privado para redirigir según el rol del usuario autenticado
    private IActionResult RedirectByRole() => Auth.GetUserRole() switch
    {
        "SuperAdmin"    => RedirectToAction("Dashboard", "SuperAdmin"),
        "Administrador" => RedirectToAction("Dashboard", "Admin"),
        "Doctor"        => RedirectToAction("Dashboard", "Medico"),
        "Paciente"      => RedirectToAction("Dashboard", "Paciente"),
        _               => RedirectToAction("Login")
    };
}
