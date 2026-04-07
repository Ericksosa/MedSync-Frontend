using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class MedicoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Exequatur { get; set; } = "";
    public int EspecialidadId { get; set; }
    public string NombreEspecialidad { get; set; } = "";
    public int HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";
    public bool EstaActivo { get; set; }
}

public class MedicoCrearViewModel
{
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Apellido { get; set; } = "";
    [Required] public string Exequatur { get; set; } = "";
    [Required] public int EspecialidadId { get; set; }
    [Required] public int HospitalId { get; set; }
}

public class MedicoEstadoViewModel
{
    public bool EstaActivo { get; set; }
}

public class DisponibilidadViewModel
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public string NombreMedico { get; set; } = "";
    public int? HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; } = "";
    public string HoraFin { get; set; } = "";

    public string NombreDia => DiaSemana switch
    {
        0 => "Lunes",
        1 => "Martes",
        2 => "Miércoles",
        3 => "Jueves",
        4 => "Viernes",
        5 => "Sábado",
        6 => "Domingo",
        _ => "?"
    };
}

public class DisponibilidadCrearViewModel
{
    [Required] public int MedicoId { get; set; }
    public int? HospitalId { get; set; }
    [Required, Range(0, 6)] public int DiaSemana { get; set; }
    [Required] public string HoraInicio { get; set; } = "";
    [Required] public string HoraFin { get; set; } = "";
}

public class HospitalAsignadoViewModel
{
    public int HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";
}

public class HospitalesAsignadosActualizarViewModel
{
    [Required] public List<int> HospitalesIds { get; set; } = [];
}
