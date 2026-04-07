using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class PagoViewModel
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public DateTime FechaCita { get; set; }
    public string NombrePaciente { get; set; } = "";
    public string NombreMedico { get; set; } = "";
    public string NombreHospital { get; set; } = "";
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime FechaPago { get; set; }
}

public class PagoCrearViewModel
{
    [Required] public int CitaId { get; set; }
    [Required, Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    [Required] public string MetodoPago { get; set; } = "";
    public string Estado { get; set; } = "Pending";
}

public class PagoEstadoViewModel
{
    [Required] public string Estado { get; set; } = "";
}

public class CobroViewModel
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public string NombreMedico { get; set; } = "";
    public int HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";
    public string Periodo { get; set; } = "";
    public decimal Monto { get; set; }
    public string Estado { get; set; } = "";
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaPago { get; set; }
}

public class CobroCrearViewModel
{
    [Required] public int MedicoId { get; set; }
    [Required] public int HospitalId { get; set; }
    [Required] public string Periodo { get; set; } = "";
    [Required, Range(0.01, double.MaxValue)] public decimal Monto { get; set; }
    [Required] public string Estado { get; set; } = "Pendiente";
}

public class BalanceMedicoViewModel
{
    public string NombreMedico { get; set; } = "";
    public decimal TotalGenerado { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Pendiente => TotalCobrado - TotalPagado;
}

public class ReporteIngresosViewModel
{
    public int HospitalId { get; set; }
    public string NombreHospital { get; set; } = "";
    public decimal TotalIngresos { get; set; }
    public int TotalCitas { get; set; }
    public int CitasCompletadas { get; set; }
    public decimal PromedioPorCita => CitasCompletadas > 0 ? TotalIngresos / CitasCompletadas : 0;
    public List<PagoViewModel> Pagos { get; set; } = [];
}
