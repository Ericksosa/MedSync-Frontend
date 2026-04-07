using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class ExpedienteViewModel
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string NombrePaciente { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public int? RitmoCardiaco { get; set; }
    public decimal? Temperatura { get; set; }
    public string? PresionArterial { get; set; }
    public DateTime? FechaHoraUltimaActualizacionSignos { get; set; }
    public IEnumerable<DiagnosticoViewModel> Diagnosticos { get; set; } = [];
    public IEnumerable<RecetaViewModel> Recetas { get; set; } = [];
}

public class SignosVitalesActualizarViewModel
{
    public int? RitmoCardiaco { get; set; }
    public decimal? Temperatura { get; set; }
    public string? PresionArterial { get; set; }
}

public class DiagnosticoViewModel
{
    public int Id { get; set; }
    public int ExpedienteId { get; set; }
    public string Descripcion { get; set; } = "";
    public int MedicoId { get; set; }
    public string NombreMedico { get; set; } = "";
    public int? CitaId { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class DiagnosticoCrearViewModel
{
    [Required] public int ExpedienteId { get; set; }
    [Required] public string Descripcion { get; set; } = "";
    [Required] public int MedicoId { get; set; }
    public int? CitaId { get; set; }
}

public class RecetaViewModel
{
    public int Id { get; set; }
    public int ExpedienteId { get; set; }
    public string Medicamento { get; set; } = "";
    public string Dosis { get; set; } = "";
    public string? Instrucciones { get; set; }
    public int MedicoId { get; set; }
    public string NombreMedico { get; set; } = "";
    public int? CitaId { get; set; }
    public DateTime FechaEmision { get; set; }
    public bool EstaActiva { get; set; }
}

public class RecetaCrearViewModel
{
    [Required] public int ExpedienteId { get; set; }
    [Required] public string Medicamento { get; set; } = "";
    [Required] public string Dosis { get; set; } = "";
    public string? Instrucciones { get; set; }
    [Required] public int MedicoId { get; set; }
    public int? CitaId { get; set; }
}
