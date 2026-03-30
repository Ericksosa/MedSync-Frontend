using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class PagoViewModel
{
    public int Id { get; set; }
    public int CitaId { get; set; }
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
    [Required] public string Estado { get; set; } = "";
}
