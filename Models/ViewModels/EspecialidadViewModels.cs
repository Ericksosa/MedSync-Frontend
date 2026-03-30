using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class EspecialidadViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";
}

public class EspecialidadCrearViewModel
{
    [Required] public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    [Required] public int HospitalId { get; set; }
}
