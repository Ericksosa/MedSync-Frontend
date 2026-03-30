using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class CitaViewModel
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = "";
    public string? Notas { get; set; }
    public int PacienteId { get; set; }
    public string NombrePaciente { get; set; } = "";
    public int MedicoId { get; set; }
    public string NombreMedico { get; set; } = "";
    public int HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";

    public string EstadoBadgeClass => Estado switch
    {
        "Pendiente"    => "bg-yellow-100 text-yellow-800",
        "Confirmada"   => "bg-blue-100 text-blue-800",
        "En Progreso"  => "bg-green-100 text-green-800",
        "Completada"   => "bg-gray-100 text-gray-700",
        "Cancelada"    => "bg-red-100 text-red-800",
        "No Presentó"  => "bg-orange-100 text-orange-800",
        _              => "bg-gray-100 text-gray-600"
    };
}

public class CitaCrearViewModel
{
    [Required] public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    [Required] public int PacienteId { get; set; }
    [Required] public int MedicoId { get; set; }
    [Required] public int HospitalId { get; set; }
}

public class CitaEstadoViewModel
{
    [Required] public string Estado { get; set; } = "";
}
