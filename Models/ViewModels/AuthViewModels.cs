using System.ComponentModel.DataAnnotations;

namespace MedSync_Frontend.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Contrasena { get; set; } = "";
}

public class TokenRespuestaViewModel
{
    public string Token { get; set; } = "";
    public DateTime Expiracion { get; set; }
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
}

public class RegistroViewModel
{
    [Required] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Contrasena { get; set; } = "";
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Apellido { get; set; } = "";
    [Required] public string Rol { get; set; } = "";
}
