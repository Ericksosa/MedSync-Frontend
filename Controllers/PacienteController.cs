using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

/// <summary>
/// Controlador para las acciones del módulo de paciente.
/// </summary>
public class PacienteController : BaseController
{
    /// <summary>
    /// Constructor que recibe los servicios de API y autenticación.
    /// </summary>
    public PacienteController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    private IActionResult CheckAccess()
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();
        if (Auth.GetUserRole() != "Paciente") return Forbid();
        return null!;
    }

    /// <summary>
    /// Muestra el dashboard principal del paciente con próximas citas y actividad reciente.
    /// </summary>
    /// <returns>Vista del dashboard de paciente.</returns>
    public async Task<IActionResult> Dashboard()
    {
        var check = CheckAccess(); if (check != null) return check;

        var userId = Auth.GetUserId()!;

        // Obtiene el registro del paciente por email para obtener el ID
        var emailResult = await Api.GetAsync<PacienteViewModel>($"/api/patients/documento/{Uri.EscapeDataString(Auth.GetUserEmail()!)}");
        if (!emailResult.Success)
        {
            // Si no se puede obtener el paciente, muestra la vista vacía
            ViewBag.NombrePaciente = Auth.GetUserName();
            ViewBag.Citas = new List<CitaViewModel>();
            return View();
        }

        var paciente = emailResult.Data!;
        var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/paciente/{paciente.Id}");
        var citas = citasResult.Data ?? [];

        ViewBag.Paciente = paciente;
        ViewBag.Citas = citas;
        ViewBag.ProximaCita = citas
            .Where(c => c.FechaHora > DateTime.Now && c.Estado != "Cancelada")
            .OrderBy(c => c.FechaHora)
            .FirstOrDefault();
        ViewBag.Recientes = citas.OrderByDescending(c => c.FechaHora).Take(5).ToList();

        return View();
    }

    /// <summary>
    /// Muestra el formulario para reservar una nueva cita.
    /// </summary>
    /// <returns>Vista para reservar cita.</returns>
    [HttpGet]
    public async Task<IActionResult> ReservarCita()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        ViewBag.Hospitales = hospitalesResult.Data ?? [];

        // Obtiene información del paciente para prellenar el formulario
        var emailResult = await Api.GetAsync<PacienteViewModel>($"/api/patients/documento/{Uri.EscapeDataString(Auth.GetUserEmail()!)}");
        ViewBag.Paciente = emailResult.Data;

        return View();
    }

    // POST /Paciente/ReservarCita
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReservarCita(CitaCrearViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        if (!ModelState.IsValid)
        {
            var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
            ViewBag.Hospitales = hospitalesResult.Data ?? [];
            return View(model);
        }

        var result = await Api.PostAsync<CitaViewModel>("/api/appointments", model);
        if (!result.Success)
        {
            SetError(result.Error ?? "Error al reservar la cita.");
            return RedirectToAction("ReservarCita");
        }

        SetSuccess("Cita reservada correctamente.");
        return RedirectToAction("Dashboard");
    }

    // GET /Paciente/Historial
    public async Task<IActionResult> Historial()
    {
        var check = CheckAccess(); if (check != null) return check;

        var emailResult = await Api.GetAsync<PacienteViewModel>($"/api/patients/documento/{Uri.EscapeDataString(Auth.GetUserEmail()!)}");
        if (!emailResult.Success)
        {
            ViewBag.Citas = new List<CitaViewModel>();
            return View();
        }

        var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/paciente/{emailResult.Data!.Id}");
        ViewBag.Citas = citasResult.Data?.OrderByDescending(c => c.FechaHora).ToList() ?? [];

        return View();
    }

    // AJAX: GET /Paciente/ObtenerEspecialidades?hospitalId=1
    [HttpGet]
    public async Task<IActionResult> ObtenerEspecialidades(int hospitalId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var result = await Api.GetAsync<List<EspecialidadViewModel>>($"/api/specialties/hospital/{hospitalId}");
        return Json(result.Data ?? []);
    }

    // AJAX: GET /Paciente/ObtenerMedicos?hospitalId=1&especialidadId=2
    [HttpGet]
    public async Task<IActionResult> ObtenerMedicos(int hospitalId, int especialidadId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var result = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{hospitalId}/especialidad/{especialidadId}");
        return Json(result.Data ?? []);
    }

    // AJAX: GET /Paciente/ObtenerDisponibilidad?medicoId=1
    [HttpGet]
    public async Task<IActionResult> ObtenerDisponibilidad(int medicoId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var result = await Api.GetAsync<List<DisponibilidadViewModel>>($"/api/doctors/{medicoId}/disponibilidad");
        return Json(result.Data ?? []);
    }
}
