using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

/// <summary>
/// Controlador para las acciones del administrador.
/// </summary>
public class AdminController : BaseController
{
    /// <summary>
    /// Constructor que recibe los servicios de API y autenticación.
    /// </summary>
    public AdminController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    private IActionResult? CheckAccess()
    {
        if (!Auth.IsAuthenticated()) return RedirectToLogin();
        if (Auth.GetUserRole() != "Administrador") return Forbid();
        return null;
    }

    /// <summary>
    /// Muestra el dashboard principal del administrador con estadísticas generales.
    /// </summary>
    /// <returns>Vista del dashboard de administrador.</returns>
    public async Task<IActionResult> Dashboard()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals");
        var hospitales = hospitalesResult.Data ?? [];

        // Estadísticas agregadas de todos los hospitales
        int totalCitas = 0;
        int totalMedicos = 0;

        foreach (var h in hospitales.Where(h => h.EstaActivo))
        {
            var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/hospital/{h.Id}");
            if (citasResult.Success) totalCitas += citasResult.Data?.Count ?? 0;

            var medicosResult = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{h.Id}");
            if (medicosResult.Success) totalMedicos += medicosResult.Data?.Count(m => m.EstaActivo) ?? 0;
        }

        ViewBag.Hospitales = hospitales;
        ViewBag.TotalCitas = totalCitas;
        ViewBag.TotalMedicos = totalMedicos;

        return View();
    }

    /// <summary>
    /// Muestra la lista de hospitales registrados.
    /// </summary>
    /// <returns>Vista con la lista de hospitales.</returns>
    public async Task<IActionResult> Hospitales()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals");
        ViewBag.Hospitales = hospitalesResult.Data ?? [];

        return View();
    }

    /// <summary>
    /// Crea un nuevo hospital a partir del modelo recibido.
    /// </summary>
    /// <param name="model">Datos del hospital a crear.</param>
    /// <returns>Redirección o vista según resultado.</returns>
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

    // ─── Especialidades ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ObtenerMedicosPorHospital(int hospitalId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var result = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{hospitalId}");
        return Json(result.Data ?? []);
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

    // ─── Médicos ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Medicos()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        var hospitales = hospitalesResult.Data ?? [];

        var medicosAll = new List<MedicoViewModel>();
        foreach (var h in hospitales)
        {
            var r = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{h.Id}");
            if (r.Success && r.Data != null) medicosAll.AddRange(r.Data);
        }

        ViewBag.Medicos = medicosAll
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre)
            .ToList();
        ViewBag.Hospitales = hospitales;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearMedico(MedicoCrearViewModel medicoModel, RegistroViewModel registroModel)
    {
        var check = CheckAccess(); if (check != null) return check;

        // 1. Create doctor entity
        var medicoResult = await Api.PostAsync<MedicoViewModel>("/api/doctors", medicoModel);
        if (!medicoResult.Success) { SetError(medicoResult.Error ?? "Error al crear médico."); return RedirectToAction("Medicos"); }

        // 2. Create user account for the doctor
        registroModel.Rol = "Doctor";
        var authResult = await Api.PostAsync<TokenRespuestaViewModel>("/api/auth/registro", registroModel);
        if (!authResult.Success) SetError($"Médico creado pero error al crear cuenta: {authResult.Error}");
        else SetSuccess($"Dr. {medicoModel.Nombre} {medicoModel.Apellido} registrado.");

        return RedirectToAction("Medicos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMedico(int id, bool estaActivo)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PatchAsync<object>($"/api/doctors/{id}/estado", new MedicoEstadoViewModel { EstaActivo = estaActivo });
        if (!result.Success) SetError(result.Error ?? "Error al actualizar estado.");
        else SetSuccess(estaActivo ? "Médico activado." : "Médico desactivado.");

        return RedirectToAction("Medicos");
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerHospitalesAsignadosMedico(int medicoId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();

        var result = await Api.GetAsync<List<HospitalAsignadoViewModel>>($"/api/doctors/{medicoId}/hospitales");
        return Json(result.Data ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarHospitalesMedico(int medicoId, List<int> hospitalesIds)
    {
        var check = CheckAccess(); if (check != null) return check;

        var payload = new HospitalesAsignadosActualizarViewModel
        {
            HospitalesIds = hospitalesIds
        };

        var result = await Api.PutAsync<object>($"/api/doctors/{medicoId}/hospitales", payload);
        if (!result.Success) SetError(result.Error ?? "No se pudieron actualizar los hospitales del médico.");
        else SetSuccess("Hospitales del médico actualizados.");

        return RedirectToAction("Medicos");
    }

    // ─── Disponibilidad ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ObtenerDisponibilidad(int medicoId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        var result = await Api.GetAsync<List<DisponibilidadViewModel>>($"/api/doctors/{medicoId}/disponibilidad");
        return Json(result.Data ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearDisponibilidad(DisponibilidadCrearViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PostAsync<DisponibilidadViewModel>("/api/doctors/disponibilidad", model);
        if (!result.Success) SetError(result.Error ?? "Error al crear disponibilidad.");
        else SetSuccess("Horario agregado.");

        return RedirectToAction("Medicos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDisponibilidad(int id)
    {
        var check = CheckAccess(); if (check != null) return check;

        var ok = await Api.DeleteAsync($"/api/doctors/disponibilidad/{id}");
        if (!ok) SetError("Error al eliminar horario.");

        return RedirectToAction("Medicos");
    }

    // ─── Pacientes ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Pacientes()
    {
        var check = CheckAccess(); if (check != null) return check;

        var pacientesResult = await Api.GetAsync<List<PacienteViewModel>>("/api/patients");
        ViewBag.Pacientes = pacientesResult.Data ?? [];

        if (!pacientesResult.Success)
            SetError(pacientesResult.Error ?? "No se pudo cargar el listado de pacientes.");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPaciente(PacienteCrearViewModel model, string emailCuenta, string passwordCuenta)
    {
        var check = CheckAccess(); if (check != null) return check;

        // 1. Create patient entity
        var pacResult = await Api.PostAsync<PacienteViewModel>("/api/patients", model);
        if (!pacResult.Success) { SetError(pacResult.Error ?? "Error al registrar paciente."); return RedirectToAction("Pacientes"); }

        // 2. Create user account
        var regResult = await Api.PostAsync<TokenRespuestaViewModel>("/api/auth/registro", new
        {
            email = emailCuenta,
            contrasena = passwordCuenta,
            nombre = model.Nombre,
            apellido = model.Apellido,
            rol = "Paciente"
        });
        if (!regResult.Success) SetError($"Paciente creado pero error al crear cuenta: {regResult.Error}");
        else SetSuccess($"Paciente {model.Nombre} {model.Apellido} registrado.");

        return RedirectToAction("Pacientes");
    }

    // ─── Citas ───────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Citas()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        var hospitales = hospitalesResult.Data ?? [];

        var citasAll = new List<CitaViewModel>();
        foreach (var h in hospitales)
        {
            var r = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/hospital/{h.Id}");
            if (r.Success && r.Data != null) citasAll.AddRange(r.Data);
        }

        ViewBag.Citas = citasAll.OrderByDescending(c => c.FechaHora).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoCita(int citaId, string estado)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PatchAsync<object>($"/api/appointments/{citaId}/estado", new CitaEstadoViewModel { Estado = estado });
        if (!result.Success) SetError(result.Error ?? "Error al cambiar estado.");
        else SetSuccess($"Cita actualizada a '{estado}'.");

        return RedirectToAction("Citas");
    }

    // ─── Historial ───────────────────────────────────────────────────────────────
    public IActionResult Historial()
    {
        var check = CheckAccess(); if (check != null) return check;
        return RedirectToAction("Citas");
    }

    // ─── Pagos ───────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Pagos()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        var hospitales = hospitalesResult.Data ?? [];

        var pagosAll = new List<PagoViewModel>();
        foreach (var h in hospitales)
        {
            var r = await Api.GetAsync<List<PagoViewModel>>($"/api/payments/hospital/{h.Id}");
            if (r.Success && r.Data != null) pagosAll.AddRange(r.Data);
        }

        ViewBag.Pagos = pagosAll.OrderByDescending(p => p.FechaPago).ToList();
        ViewBag.Hospitales = hospitales;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerCitasPagablesPorHospital(int hospitalId)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();

        var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/hospital/{hospitalId}");
        var pagosResult = await Api.GetAsync<List<PagoViewModel>>($"/api/payments/hospital/{hospitalId}");

        var citas = citasResult.Data ?? [];
        var pagos = pagosResult.Data ?? [];
        var citasConPago = pagos.Select(p => p.CitaId).ToHashSet();

        var citasDisponibles = citas
            .Where(c => c.Estado != "Cancelada" && c.Estado != "No Presentó" && !citasConPago.Contains(c.Id))
            .OrderBy(c => c.FechaHora)
            .ToList();

        return Json(citasDisponibles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPago(PagoCrearViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        model.Estado = "Pending";
        var result = await Api.PostAsync<PagoViewModel>("/api/payments", model);
        if (!result.Success) SetError(result.Error ?? "Error al registrar pago.");
        else SetSuccess("Pago registrado.");

        return RedirectToAction("Pagos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarPagoCompletado(int id)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PatchAsync<object>($"/api/payments/{id}/estado", new PagoEstadoViewModel { Estado = "Completed" });
        if (!result.Success) SetError(result.Error ?? "Error al confirmar pago.");
        else SetSuccess("Pago marcado como completado.");

        return RedirectToAction("Pagos");
    }

    // ─── Cobros (RF17) ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Cobros()
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        var hospitales = hospitalesResult.Data ?? [];

        var cobrosAll = new List<CobroViewModel>();
        var erroresCobros = new List<string>();
        foreach (var h in hospitales)
        {
            var r = await Api.GetAsync<List<CobroViewModel>>($"/api/cobros/hospital/{h.Id}");
            if (r.Success && r.Data != null) cobrosAll.AddRange(r.Data);
            else if (!r.Success)
                erroresCobros.Add($"{h.Nombre} (HTTP {r.StatusCode})");
        }

        ViewBag.Cobros = cobrosAll.OrderByDescending(c => c.FechaEmision).ToList();
        ViewBag.Hospitales = hospitales;
        ViewBag.CobrosEndpointDisponible = !erroresCobros.Any();

        if (erroresCobros.Any())
            SetError($"No se pudo consultar cobros en algunos hospitales: {string.Join(", ", erroresCobros)}.");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarCobro(CobroCrearViewModel model)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PostAsync<CobroViewModel>("/api/cobros", model);
        if (!result.Success) SetError(result.Error ?? "Error al registrar cobro.");
        else SetSuccess("Cobro registrado correctamente.");

        return RedirectToAction("Cobros");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarCobroPagado(int id)
    {
        var check = CheckAccess(); if (check != null) return check;

        var result = await Api.PatchAsync<object>($"/api/cobros/{id}/estado", new { estado = "Pagado" });
        if (!result.Success) SetError(result.Error ?? "Error al actualizar cobro.");
        else SetSuccess("Cobro marcado como pagado.");

        return RedirectToAction("Cobros");
    }

    // ─── Estado de Cuenta del Doctor (RF18) ──────────────────────────────────────
    public async Task<IActionResult> EstadoCuenta(int? hospitalId)
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        var hospitales = hospitalesResult.Data ?? [];
        ViewBag.Hospitales = hospitales;
        ViewBag.SelectedHospital = hospitalId;

        if (hospitalId.HasValue)
        {
            var medicosResult = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{hospitalId}");
            var medicos = medicosResult.Data ?? [];

            var balances = new List<BalanceMedicoViewModel>();
            foreach (var m in medicos)
            {
                var balResult = await Api.GetAsync<BalanceMedicoViewModel>($"/api/cobros/doctor/{m.Id}/balance");
                if (balResult.Success && balResult.Data != null)
                {
                    balResult.Data.NombreMedico = m.NombreCompleto;
                    balances.Add(balResult.Data);
                }
                else
                {
                    balances.Add(new BalanceMedicoViewModel { NombreMedico = m.NombreCompleto });
                }
            }
            ViewBag.Balances = balances;
        }

        return View();
    }

    // ─── Reportes ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Reportes(int? hospitalId, string? desde, string? hasta)
    {
        var check = CheckAccess(); if (check != null) return check;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        ViewBag.Hospitales = hospitalesResult.Data ?? [];

        if (hospitalId.HasValue && !string.IsNullOrWhiteSpace(desde) && !string.IsNullOrWhiteSpace(hasta))
        {
            var result = await Api.GetAsync<ReporteIngresosViewModel>($"/api/reports/hospital/{hospitalId}/ingresos?desde={desde}&hasta={hasta}");
            ViewBag.Reporte = result.Data;
            ViewBag.SelectedHospital = hospitalId;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
        }

        return View();
    }
}
