using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class PacienteViewModel
{
    public int Id { get; set; }
    public string DocumentoId { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public DateTime FechaNacimiento { get; set; }

    public int Edad => (int)((DateTime.Today - FechaNacimiento).TotalDays / 365.25);
}

public class PacienteCrearViewModel
{
    [Required] public string DocumentoId { get; set; } = "";
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Apellido { get; set; } = "";
    [EmailAddress] public string? Email { get; set; }
    public string? Telefono { get; set; }
    [Required] public DateTime FechaNacimiento { get; set; }
}

public class PacienteActualizarViewModel
{
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Apellido { get; set; } = "";
    [EmailAddress] public string? Email { get; set; }
    public string? Telefono { get; set; }
}
