using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MedSync_Frontend.Models.ViewModels;
using MedSync_Frontend.Services;

namespace MedSync_Frontend.Controllers;

public class NotificacionesController : BaseController
{
    public NotificacionesController(ApiClient api, AuthTokenService auth) : base(api, auth) { }

    [HttpGet]
    public async Task<IActionResult> Feed()
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();

        var role = Auth.GetUserRole() ?? string.Empty;
        var items = role switch
        {
            "Paciente" => await BuildPacienteItems(),
            "Doctor" => await BuildDoctorItems(),
            "Administrador" => await BuildAdministradorItems(),
            "SuperAdmin" => await BuildSuperAdminItems(),
            _ => new List<NotificacionItemViewModel>()
        };

        var readIds = GetReadIds();
        var markAllRead = readIds.Contains("*");
        foreach (var item in items)
            item.Leida = markAllRead || readIds.Contains(item.Id);

        var ordered = items.OrderByDescending(i => i.Fecha ?? DateTime.MinValue).Take(12).ToList();

        var response = new NotificacionFeedViewModel
        {
            Items = ordered,
            NoLeidas = ordered.Count(i => !i.Leida)
        };

        return Json(response);
    }

    [HttpPost]
    public IActionResult MarcarLeida([FromBody] MarcarNotificacionRequest request)
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Id)) return BadRequest();

        var read = GetReadIds();
        read.Add(request.Id);
        SaveReadIds(read);

        return Ok(new { ok = true });
    }

    [HttpPost]
    public IActionResult MarcarTodasLeidas()
    {
        if (!Auth.IsAuthenticated()) return Unauthorized();

        HttpContext.Session.SetString(GetReadKey(), JsonSerializer.Serialize(new HashSet<string> { "*" }));
        return Ok(new { ok = true });
    }

    private async Task<List<NotificacionItemViewModel>> BuildPacienteItems()
    {
        var items = new List<NotificacionItemViewModel>();
        var email = Auth.GetUserEmail();
        if (string.IsNullOrWhiteSpace(email)) return items;

        var pacienteResult = await Api.GetAsync<PacienteViewModel>($"/api/patients/email/{Uri.EscapeDataString(email)}");
        if (!pacienteResult.Success || pacienteResult.Data is null) return items;

        var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/paciente/{pacienteResult.Data.Id}");
        var citas = citasResult.Data ?? [];

        var proximas = citas
            .Where(c => c.FechaHora >= DateTime.Now && c.Estado != "Cancelada" && c.Estado != "Completada")
            .OrderBy(c => c.FechaHora)
            .Take(5);

        foreach (var cita in proximas)
        {
            items.Add(new NotificacionItemViewModel
            {
                Id = $"pac-prox-{cita.Id}",
                Tipo = "recordatorio",
                Titulo = "Cita proxima",
                Mensaje = $"{cita.NombreMedico} en {cita.NombreHospital} el {cita.FechaHora:dd/MM/yyyy HH:mm}.",
                Link = "/Paciente/Historial",
                Fecha = cita.FechaHora
            });
        }

        var cambios = citas
            .Where(c => c.Estado == "Cancelada" || c.Estado == "No Presentó")
            .OrderByDescending(c => c.FechaHora)
            .Take(2);

        foreach (var cita in cambios)
        {
            items.Add(new NotificacionItemViewModel
            {
                Id = $"pac-estado-{cita.Id}-{cita.Estado}",
                Tipo = "estado",
                Titulo = "Actualizacion de cita",
                Mensaje = $"Tu cita del {cita.FechaHora:dd/MM/yyyy HH:mm} esta en estado: {cita.Estado}.",
                Link = "/Paciente/Historial",
                Fecha = cita.FechaHora
            });
        }

        return items;
    }

    private async Task<List<NotificacionItemViewModel>> BuildDoctorItems()
    {
        var items = new List<NotificacionItemViewModel>();
        var medicoId = await ResolveMedicoId();
        if (!medicoId.HasValue) return items;

        var hoy = DateTime.Today.ToString("yyyy-MM-dd");
        var agendaResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/medico/{medicoId}/agenda?fecha={hoy}");
        var citas = agendaResult.Data ?? [];

        foreach (var cita in citas.Where(c => c.Estado != "Completada").OrderBy(c => c.FechaHora).Take(6))
        {
            items.Add(new NotificacionItemViewModel
            {
                Id = $"doc-hoy-{cita.Id}",
                Tipo = "agenda",
                Titulo = "Agenda de hoy",
                Mensaje = $"{cita.NombrePaciente} a las {cita.FechaHora:HH:mm} ({cita.Estado}).",
                Link = "/Medico/Agenda",
                Fecha = cita.FechaHora
            });
        }

        var pendientes = citas.Count(c => c.Estado == "Pendiente" || c.Estado == "Confirmada");
        if (pendientes > 0)
        {
            items.Add(new NotificacionItemViewModel
            {
                Id = $"doc-pend-{DateTime.Today:yyyyMMdd}",
                Tipo = "resumen",
                Titulo = "Pendientes del dia",
                Mensaje = $"Tienes {pendientes} cita(s) pendiente(s) por atender hoy.",
                Link = "/Medico/Dashboard",
                Fecha = DateTime.Now
            });
        }

        return items;
    }

    private async Task<List<NotificacionItemViewModel>> BuildAdministradorItems()
    {
        var items = new List<NotificacionItemViewModel>();
        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        var hospitales = hospitalesResult.Data ?? [];

        var totalCitasPendientes = 0;
        foreach (var hospital in hospitales)
        {
            var citasResult = await Api.GetAsync<List<CitaViewModel>>($"/api/appointments/hospital/{hospital.Id}");
            var pendientes = (citasResult.Data ?? []).Count(c => c.Estado != "Completada" && c.Estado != "Cancelada");
            totalCitasPendientes += pendientes;

            if (pendientes > 0)
            {
                items.Add(new NotificacionItemViewModel
                {
                    Id = $"adm-hospital-{hospital.Id}-{DateTime.Today:yyyyMMdd}",
                    Tipo = "hospital",
                    Titulo = "Citas pendientes por hospital",
                    Mensaje = $"{hospital.Nombre}: {pendientes} cita(s) pendiente(s).",
                    Link = "/Admin/Citas",
                    Fecha = DateTime.Now
                });
            }
        }

        items.Add(new NotificacionItemViewModel
        {
            Id = $"adm-total-{DateTime.Today:yyyyMMdd}",
            Tipo = "resumen",
            Titulo = "Resumen general",
            Mensaje = $"Total de citas activas en la red: {totalCitasPendientes}.",
            Link = "/Admin/Dashboard",
            Fecha = DateTime.Now
        });

        return items;
    }

    private async Task<List<NotificacionItemViewModel>> BuildSuperAdminItems()
    {
        var items = new List<NotificacionItemViewModel>();
        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals");
        var hospitales = hospitalesResult.Data ?? [];

        items.Add(new NotificacionItemViewModel
        {
            Id = $"sup-hosp-{DateTime.Today:yyyyMMdd}",
            Tipo = "resumen",
            Titulo = "Estado de la red",
            Mensaje = $"Hospitales activos: {hospitales.Count(h => h.EstaActivo)} de {hospitales.Count}.",
            Link = "/SuperAdmin/Hospitales",
            Fecha = DateTime.Now
        });

        return items;
    }

    private async Task<int?> ResolveMedicoId()
    {
        var cached = HttpContext.Session.GetString("medicoId");
        if (!string.IsNullOrWhiteSpace(cached) && int.TryParse(cached, out var cachedId)) return cachedId;

        var hospitalesResult = await Api.GetAsync<List<HospitalViewModel>>("/api/hospitals/activos");
        if (!hospitalesResult.Success) return null;

        var userName = Auth.GetUserName() ?? string.Empty;
        var nameParts = userName.Split(' ', 2);

        foreach (var hospital in hospitalesResult.Data ?? [])
        {
            var medicosResult = await Api.GetAsync<List<MedicoViewModel>>($"/api/doctors/hospital/{hospital.Id}");
            if (!medicosResult.Success) continue;

            var match = medicosResult.Data?.FirstOrDefault(m =>
                m.NombreCompleto.Equals(userName, StringComparison.OrdinalIgnoreCase) ||
                (nameParts.Length >= 1 && m.Nombre.Equals(nameParts[0], StringComparison.OrdinalIgnoreCase)));

            if (match is not null)
            {
                HttpContext.Session.SetString("medicoId", match.Id.ToString());
                return match.Id;
            }
        }

        return null;
    }

    private string GetReadKey()
    {
        var role = Auth.GetUserRole() ?? "anon";
        var userId = Auth.GetUserId() ?? Auth.GetUserEmail() ?? "anon";
        return $"notif-read::{role}::{userId}";
    }

    private HashSet<string> GetReadIds()
    {
        var raw = HttpContext.Session.GetString(GetReadKey());
        if (string.IsNullOrWhiteSpace(raw)) return new HashSet<string>();

        try
        {
            var ids = JsonSerializer.Deserialize<HashSet<string>>(raw) ?? new HashSet<string>();
            if (ids.Contains("*")) return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "*"
            };
            return ids;
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private void SaveReadIds(HashSet<string> ids)
    {
        HttpContext.Session.SetString(GetReadKey(), JsonSerializer.Serialize(ids));
    }
}
