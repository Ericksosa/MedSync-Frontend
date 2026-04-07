using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

public class SuperAdminController : BaseController
{
    public SuperAdminController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    private IActionResult? CheckAccess()
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();
        if (Auth.GetUserRole() != "SuperAdmin") return Forbid();
        return null;
    }

    // ─── Dashboard ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Dashboard()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals");
        var hospitales = hospitalesResult.Data ?? [];

        int totalCitas = 0;
        int totalMedicos = 0;

        foreach (var h in hospitales.Where(h => h.EstaActivo))
        {
            var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/hospital/{h.Id}");
            if (citasResult.Success) totalCitas += citasResult.Data?.Count ?? 0;

            var medicosResult = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{h.Id}");
            if (medicosResult.Success) totalMedicos += medicosResult.Data?.Count(m => m.EstaActivo) ?? 0;
        }

        // Intentar obtener total de pacientes
        var pacientesResult = await Api.GetAsync<List<PacienteViewModel>>("/api/patients");
        var totalPacientes = pacientesResult.Data?.Count ?? 0;

        // Intentar listar administradores
        var adminsResult = await Api.GetAsync<List<UsuarioViewModel>>("/api/auth/users?rol=Administrador");
        var admins = adminsResult.Data ?? [];

        ViewBag.Hospitales = hospitales;
        ViewBag.TotalCitas = totalCitas;
        ViewBag.TotalMedicos = totalMedicos;
        ViewBag.TotalPacientes = totalPacientes;
        ViewBag.Administradores = admins;

        return View();
    }

    // ─── Administradores ─────────────────────────────────────────────────────────
    public async Task<IActionResult> Administradores()
    {
        var check = CheckAccess(); if (check != null) return check;

        var adminsResult = await Api.GetAsync<List<UsuarioViewModel>>("/api/auth/users?rol=Administrador");
        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals");

        ViewBag.Administradores = adminsResult.Data ?? [];
        ViewBag.Hospitales = hospitalesResult.Data ?? [];

        if (!adminsResult.Success)
            SetError("No se pudo cargar la lista de administradores. Verifique que el endpoint esté disponible en el backend.");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAdministrador(RegistroViewModel registroModel, int? hospitalId)
    {
        var check = CheckAccess(); if (check != null) return check;

        registroModel.Rol = "Administrador";
        var authResult = await Api.PostAsync<TokenRespuestaViewModel>("/api/auth/registro", registroModel);

        if (!authResult.Success)
            SetError(authResult.Error ?? "Error al crear administrador.");
        else
            SetSuccess($"Administrador {registroModel.Nombre} {registroModel.Apellido} creado correctamente.");

        return RedirectToAction("Administradores");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdministrador(string userId, bool estaActivo)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PatchAsync<object>($"/api/auth/users/{userId}/estado", new { estaActivo });
        if (!result.Success) SetError(result.Error ?? "Error al actualizar estado del administrador.");
        else SetSuccess(estaActivo ? "Administrador activado." : "Administrador desactivado.");

        return RedirectToAction("Administradores");
    }

    // ─── Hospitales (visión global) ───────────────────────────────────────────────
    public async Task<IActionResult> Hospitales()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals");
        ViewBag.Hospitales = hospitalesResult.Data ?? [];

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearHospital(HospitalCrearViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PostAsync<HospitalViewModel>("/api/hospitals", model);
        if (!result.Success) SetError(result.Error ?? "Error al crear hospital.");
        else SetSuccess($"Hospital '{model.Nombre}' creado.");

        return RedirectToAction("Hospitales");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarHospital(int id, HospitalActualizarViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PutAsync<object>($"/api/hospitals/{id}", model);
        if (!result.Success) SetError(result.Error ?? "Error al actualizar hospital.");
        else SetSuccess("Hospital actualizado.");

        return RedirectToAction("Hospitales");
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerEspecialidades(int hospitalId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var result = await Api.GetAsync<List<EspecialidadViewModel>>($"/api/specialties/hospital/{hospitalId}");
        return Json(result.Data ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEspecialidad(EspecialidadCrearViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PostAsync<EspecialidadViewModel>("/api/specialties", model);
        if (!result.Success) SetError(result.Error ?? "Error al crear especialidad.");
        else SetSuccess($"Especialidad '{model.Nombre}' creada.");

        return RedirectToAction("Hospitales");
    }

    // ─── Usuarios (todos los del sistema) ────────────────────────────────────────
    public async Task<IActionResult> Usuarios()
    {
        var check = CheckAccess(); if (check != null) return check;

        var usuariosResult = await Api.GetAsync<List<UsuarioViewModel>>("/api/auth/users");
        ViewBag.Usuarios = usuariosResult.Data ?? [];

        if (!usuariosResult.Success)
            SetError("No se pudo cargar la lista de usuarios. Verifique que el endpoint esté disponible en el backend.");

        return View();
    }
}
