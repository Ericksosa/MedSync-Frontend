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

public class PacienteRegistroViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "El apellido es requerido")] public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "El documento es requerido")] public string DocumentoId { get; set; } = "";
    [Required(ErrorMessage = "El email es requerido"), EmailAddress] public string Email { get; set; } = "";
    public string? Telefono { get; set; }
    [Required(ErrorMessage = "La fecha de nacimiento es requerida")] public DateTime FechaNacimiento { get; set; }
    [Required(ErrorMessage = "La contraseña es requerida"), MinLength(8, ErrorMessage = "Mínimo 8 caracteres")] public string Contrasena { get; set; } = "";
}

public class UsuarioViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public string Rol { get; set; } = "";
    public bool EstaActivo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
}

public class PerfilUsuarioViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Rol { get; set; } = "";
}

public class PerfilActualizarViewModel
{
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Apellido { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
}

public class CambiarContrasenaViewModel
{
    [Required] public string ContrasenaActual { get; set; } = "";
    [Required, MinLength(8)] public string NuevaContrasena { get; set; } = "";
}
