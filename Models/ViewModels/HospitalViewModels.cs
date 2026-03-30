using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class HospitalViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Rnc { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public bool EstaActivo { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class HospitalCrearViewModel
{
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Rnc { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
}

public class HospitalActualizarViewModel
{
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Rnc { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public bool EstaActivo { get; set; }
}
