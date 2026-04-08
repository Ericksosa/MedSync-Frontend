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
        var citas = (agendaResult.Data ?? []).Where(c => c.Estado != "Completada").ToList();

        ViewBag.NombreDoctor = Auth.GetUserName();
        ViewBag.CitasHoy = citas.OrderBy(c => c.FechaHora).ToList();
        ViewBag.TotalPacientes = citas.Count;

        return View();
    }

    // GET /Medico/Agenda?fecha=2026-03-30
    [HttpGet]
    public async Task<IActionResult> Agenda(string? fecha, int? hospitalId)
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        var hospitalesAsignados = new List<HospitalAsignadoViewModel>();

        if (medicoId.HasValue)
        {
            var hospitalesAsignadosResult = await Api.GetAsync<List<HospitalAsignadoViewModel>>($"/api/doctors/{medicoId}/hospitales");
            if (hospitalesAsignadosResult.Success)
                hospitalesAsignados = hospitalesAsignadosResult.Data ?? [];
        }

        var hospitalIdSeleccionado = hospitalId.HasValue && hospitalesAsignados.Any(h => h.HospitalId == hospitalId.Value)
            ? hospitalId
            : null;

        ViewBag.MedicoId = medicoId;
        ViewBag.NombreDoctor = Auth.GetUserName();
        ViewBag.HospitalesAsignados = hospitalesAsignados;
        ViewBag.HospitalIdSeleccionado = hospitalIdSeleccionado;
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

    // POST /Medico/ActualizarFechaNacimientoPaciente
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarFechaNacimientoPaciente(
        int pacienteId,
        string nombre,
        string apellido,
        string? email,
        string? telefono,
        DateTime fechaNacimiento)
    {
        var check = CheckAccess(); if (check != null) return check;

        if (fechaNacimiento == default || fechaNacimiento > DateTime.Today)
        {
            SetError("La fecha de nacimiento indicada no es válida.");
            return RedirectToAction("Paciente", new { id = pacienteId });
        }

        var payload = new PacienteActualizarViewModel
        {
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Telefono = telefono,
            FechaNacimiento = fechaNacimiento
        };

        var result = await Api.PutAsync<object>($"/api/patients/{pacienteId}", payload);
        if (!result.Success) SetError(result.Error ?? "No se pudo actualizar la fecha de nacimiento.");
        else SetSuccess("Fecha de nacimiento actualizada.");

        return RedirectToAction("Paciente", new { id = pacienteId });
    }

    // POST /Medico/ActualizarSignosVitales
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarSignosVitales(
        int expedienteId,
        int pacienteId,
        int? ritmoCardiaco,
        decimal? temperatura,
        string? presionArterial)
    {
        var check = CheckAccess(); if (check != null) return check;

        var payload = new SignosVitalesActualizarViewModel
        {
            RitmoCardiaco = ritmoCardiaco,
            Temperatura = temperatura,
            PresionArterial = presionArterial
        };

        var result = await Api.PatchAsync<object>($"/api/medicalrecords/{expedienteId}/signos-vitales", payload);
        if (!result.Success) SetError(result.Error ?? "No se pudieron actualizar los signos vitales.");
        else SetSuccess("Signos vitales actualizados.");

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

    // GET /Medico/CompletarCitas?fecha=2026-04-07
    [HttpGet]
    public async Task<IActionResult> CompletarCitas(string? fecha)
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        if (medicoId == null)
        {
            ViewBag.Citas = new List<CitaViewModel>();
            ViewBag.Fecha = DateTime.Today.ToString("yyyy-MM-dd");
            return View();
        }

        var fechaSeleccionada = DateTime.TryParse(fecha, out var f) ? f.Date : DateTime.Today;
        var fechaQuery = fechaSeleccionada.ToString("yyyy-MM-dd");

        var result = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/medico/{medicoId}/agenda?fecha={fechaQuery}");
        ViewBag.Citas = result.Data?.OrderBy(c => c.FechaHora).ToList() ?? [];
        ViewBag.Fecha = fechaQuery;

        return View();
    }

    // GET /Medico/Disponibilidad
    [HttpGet]
    public async Task<IActionResult> Disponibilidad(int? hospitalId)
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        if (medicoId == null)
        {
            SetError("No se encontró tu perfil de doctor.");
            ViewBag.HospitalesAsignados = new List<HospitalAsignadoViewModel>();
            ViewBag.Disponibilidades = new List<DisponibilidadViewModel>();
            return View();
        }

        var hospitalesAsignadosResult = await Api.GetAsync<List<HospitalAsignadoViewModel>>($"/api/doctors/{medicoId}/hospitales");
        if (!hospitalesAsignadosResult.Success)
        {
            SetError(hospitalesAsignadosResult.Error ?? "No se pudieron cargar tus hospitales asignados.");
            ViewBag.HospitalesAsignados = new List<HospitalAsignadoViewModel>();
            ViewBag.Disponibilidades = new List<DisponibilidadViewModel>();
            return View();
        }

        var hospitalesAsignados = hospitalesAsignadosResult.Data ?? [];

        if (!hospitalesAsignados.Any())
        {
            SetError("No tienes hospitales asignados. Contacta al administrador.");
            ViewBag.HospitalesAsignados = new List<HospitalAsignadoViewModel>();
            ViewBag.Disponibilidades = new List<DisponibilidadViewModel>();
            return View();
        }

        var selectedHospitalId = hospitalId.HasValue && hospitalesAsignados.Any(h => h.HospitalId == hospitalId.Value)
            ? hospitalId.Value
            : hospitalesAsignados.First().HospitalId;

        var disponibilidadResult = await Api.GetAsync<List<DisponibilidadViewModel>>($"/api/doctors/{medicoId}/disponibilidad?hospitalId={selectedHospitalId}");

        ViewBag.HospitalesAsignados = hospitalesAsignados;
        ViewBag.HospitalIdSeleccionado = selectedHospitalId;
        ViewBag.NombreHospitalSeleccionado = hospitalesAsignados.First(h => h.HospitalId == selectedHospitalId).NombreHospital;
        ViewBag.MedicoIdSeleccionado = medicoId;
        ViewBag.Disponibilidades = disponibilidadResult.Data?.OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio).ToList() ?? [];

        return View();
    }

    // POST /Medico/AgregarDisponibilidad
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarDisponibilidad(int hospitalId, int diaSemana, string horaInicio, string horaFin)
    {
        var check = CheckAccess(); if (check != null) return check;

        var medicoId = await ResolveAndCacheMedicoId();
        if (medicoId == null)
        {
            SetError("No se encontró tu perfil de doctor.");
            return RedirectToAction(nameof(Disponibilidad));
        }

        var hospitalesAsignadosResult = await Api.GetAsync<List<HospitalAsignadoViewModel>>($"/api/doctors/{medicoId}/hospitales");
        if (!hospitalesAsignadosResult.Success)
        {
            SetError(hospitalesAsignadosResult.Error ?? "No se pudieron validar tus hospitales asignados.");
            return RedirectToAction(nameof(Disponibilidad));
        }

        var hospitalesAsignados = hospitalesAsignadosResult.Data ?? [];

        if (!hospitalesAsignados.Any(h => h.HospitalId == hospitalId))
        {
            SetError("El hospital seleccionado no está asignado a tu perfil.");
            return RedirectToAction(nameof(Disponibilidad));
        }

        var payload = new DisponibilidadCrearViewModel
        {
            MedicoId = medicoId.Value,
            HospitalId = hospitalId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin
        };

        var result = await Api.PostAsync<DisponibilidadViewModel>("/api/doctors/disponibilidad", payload);
        if (!result.Success) SetError(result.Error ?? "No se pudo agregar la disponibilidad.");
        else SetSuccess("Disponibilidad guardada correctamente.");

        return RedirectToAction(nameof(Disponibilidad), new { hospitalId });
    }

    // POST /Medico/EliminarDisponibilidad
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDisponibilidad(int disponibilidadId, int hospitalId)
    {
        var check = CheckAccess(); if (check != null) return check;

        var ok = await Api.DeleteAsync($"/api/doctors/disponibilidad/{disponibilidadId}");
        if (!ok) SetError("No se pudo eliminar la disponibilidad.");
        else SetSuccess("Disponibilidad eliminada.");

        return RedirectToAction(nameof(Disponibilidad), new { hospitalId });
    }
}
