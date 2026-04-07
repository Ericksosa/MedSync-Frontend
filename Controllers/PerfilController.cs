using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

public class PerfilController : BaseController
{
    public PerfilController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    public async Task<IActionResult> Index()
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();

        var perfilResult = await Api.GetAsync<PerfilUsuarioViewModel>("/api/auth/me");
        if (perfilResult.Success && perfilResult.Data is not null)
        {
            ViewBag.Nombre = $"{perfilResult.Data.Nombre} {perfilResult.Data.Apellido}".Trim();
            ViewBag.Email = perfilResult.Data.Email;
            ViewBag.Rol = perfilResult.Data.Rol;
            ViewBag.UserId = perfilResult.Data.Id;
        }
        else
        {
            ViewBag.Nombre = Auth.GetUserName();
            ViewBag.Email = Auth.GetUserEmail();
            ViewBag.Rol = Auth.GetUserRole();
            ViewBag.UserId = Auth.GetUserId();
            SetError(perfilResult.Error ?? "No se pudo cargar el perfil.");
        }

        ViewBag.EditarPerfilModel = new PerfilActualizarViewModel
        {
            Nombre = (perfilResult.Data?.Nombre ?? string.Empty),
            Apellido = (perfilResult.Data?.Apellido ?? string.Empty),
            Email = (perfilResult.Data?.Email ?? string.Empty)
        };

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarPerfil(PerfilActualizarViewModel model)
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();

        var result = await Api.PutAsync<TokenRespuestaViewModel>("/api/auth/me", model);
        if (!result.Success)
        {
            SetError(result.Error ?? "No se pudo actualizar el perfil.");
            return RedirectToAction("Index");
        }

        Auth.StoreToken(result.Data!.Token, result.Data.Email, result.Data.Rol);
        SetSuccess("Perfil actualizado correctamente.");
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel model)
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();

        var result = await Api.PostAsync<object>("/api/auth/me/cambiar-contrasena", model);
        if (!result.Success)
            SetError(result.Error ?? "No se pudo cambiar la contraseña.");
        else
            SetSuccess("Contraseña actualizada correctamente.");

        return RedirectToAction("Index");
    }
}
