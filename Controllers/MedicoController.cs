using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

/// <summary>
/// Controlador para las acciones del módulo de médico.
/// </summary>
public class MedicoController : BaseController
{
    /// <summary>
    /// Constructor que recibe los servicios de API y autenticación.
    /// </summary>
    public MedicoController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    private IActionResult? CheckAccess()
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();
        if (Auth.GetUserRole() != "Doctor") return Forbid();
        return null;
    }

    private int? GetMedicoId()
    {
        var raw = HttpContext.Session.GetString("medicoId");
        return raw is not null ? int.Parse(raw) : null;
    }

    /// <summary>
    /// Resuelve y almacena en caché el ID del médico buscando en todos los hospitales activos.
    /// </summary>
    /// <returns>ID del médico o null si no se encuentra.</returns>
    private async Task<int?> ResolveAndCacheMedicoId()
    {
        var cached = GetMedicoId();
        if (cached.HasValue) return cached;

        // Obtiene todos los hospitales activos y busca el médico por nombre
        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        if (!hospitalesResult.Success) return null;

        var userName = Auth.GetUserName() ?? "";
        var nameParts = userName.Split(' ', 2);

        foreach (var hospital in hospitalesResult.Data ?? [])
        {
            var medicosResult = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{hospital.Id}");
            if (!medicosResult.Success) continue;

            var match = medicosResult.Data?.FirstOrDefault(m =>
                m.NombreCompleto.Equals(userName, StringComparison.OrdinalIgnoreCase) ||
                (nameParts.Length >= 1 && m.Nombre.Equals(nameParts[0], StringComparison.OrdinalIgnoreCase)));

            if (match != null)
            {
                HttpContext.Session.SetString("medicoId", match.Id.ToString());
                return match.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Muestra el dashboard principal del médico.
    /// </summary>
    /// <returns>Vista del dashboard de médico.</returns>
    public async Task<IActionResult> Dashboard()
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        if (medicoId == null)
        {
            ViewBag.Citas = new List<CitaViewModel>();
            ViewBag.NombreDoctor = Auth.GetUserName();
            return View();
        }

        var hoy = DateTime.Today.ToString("yyyy-MM-dd");
        var agendaResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/medico/{medicoId}/agenda?fecha={hoy}");
        var citas = agendaResult.Data ?? [];

        ViewBag.NombreDoctor = Auth.GetUserName();
        ViewBag.CitasHoy = citas.OrderBy(c => c.FechaHora).ToList();
        ViewBag.TotalPacientes = citas.Count;

        return View();
    }

    // GET /Medico/Agenda?fecha=2026-03-30
    [HttpGet]
    public async Task<IActionResult> Agenda(string? fecha)
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        ViewBag.MedicoId = medicoId;
        ViewBag.NombreDoctor = Auth.GetUserName();
        return View();
    }

    // AJAX: GET /Medico/ObtenerAgenda?fecha=2026-03-30
    [HttpGet]
    public async Task<IActionResult> ObtenerAgenda(string fecha)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var medicoId = await ResolveAndCacheMedicoId();
        if (medicoId == null) return Json(new List<object>());

        var result = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/medico/{medicoId}/agenda?fecha={fecha}");
        var citas = result.Data ?? [];

        // FullCalendar event format
        var events = citas.Select(c => new {
            id = c.Id,
            title = c.NombrePaciente,
            start = c.FechaHora.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = c.FechaHora.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
            extendedProps = new { estado = c.Estado, notas = c.Notas, pacienteId = c.PacienteId, hospitalId = c.HospitalId }
        });

        return Json(events);
    }

    // GET /Medico/Paciente/{pacienteId}
    [HttpGet]
    public async Task<IActionResult> Paciente(int id)
    {
        var check = CheckAccess(); if (check != null) return check;

        var expedienteResult = await Api.GetAsync<ExpedienteViewModel>($"/api/medicalrecords/paciente/{id}");
        if (!expedienteResult.Success)
        {
            SetError("No se pudo cargar el expediente.");
            return RedirectToAction("Dashboard");
        }

        var pacienteResult = await Api.GetAsync<PacienteViewModel>($"/api/patients/{id}");
        ViewBag.Expediente = expedienteResult.Data;
        ViewBag.Paciente = pacienteResult.Data;
        ViewBag.MedicoId = await ResolveAndCacheMedicoId();

        return View();
    }

    // POST /Medico/RegistrarDiagnostico
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarDiagnostico(DiagnosticoCrearViewModel model, int pacienteId)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PostAsync<DiagnosticoViewModel>("/api/medicalrecords/diagnosticos", model);
        if (!result.Success) SetError(result.Error ?? "Error al registrar diagnóstico.");
        else SetSuccess("Diagnóstico registrado.");

        return RedirectToAction("Paciente", new { id = pacienteId });
    }

    // POST /Medico/EmitirReceta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmitirReceta(RecetaCrearViewModel model, int pacienteId)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PostAsync<RecetaViewModel>("/api/medicalrecords/recetas", model);
        if (!result.Success) SetError(result.Error ?? "Error al emitir receta.");
        else SetSuccess("Receta emitida.");

        return RedirectToAction("Paciente", new { id = pacienteId });
    }

    // POST /Medico/CambiarEstado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int citaId, string estado, string returnUrl = "/Medico/Dashboard")
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PatchAsync<object>($"/api/appointments/{citaId}/estado", new CitaEstadoViewModel { Estado = estado });
        if (!result.Success) SetError(result.Error ?? "Error al cambiar estado.");
        else SetSuccess($"Cita marcada como {estado}.");

        return Redirect(returnUrl);
    }

    // GET /Medico/Historial
    public async Task<IActionResult> Historial()
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        if (medicoId == null) { ViewBag.Citas = new List<CitaViewModel>(); return View(); }

        // Fetch today's agenda as a proxy for recent appointments
        var hoy = DateTime.Today.ToString("yyyy-MM-dd");
        var result = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/medico/{medicoId}/agenda?fecha={hoy}");
        ViewBag.Citas = result.Data?.OrderByDescending(c => c.FechaHora).ToList() ?? [];

        return View();
    }
}
