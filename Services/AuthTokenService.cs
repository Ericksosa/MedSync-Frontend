using System.Text;
using System.Text.Json;

namespace MedSync_Frontend.Services;

/// <summary>
/// Servicio para gestionar el token JWT y los datos de sesión del usuario autenticado.
/// </summary>
public class AuthTokenService
{
    private const string TokenKey = "jwt_token";
    private const string UserIdKey = "user_id";
    private const string UserNameKey = "user_name";
    private const string UserRoleKey = "user_role";
    private const string UserEmailKey = "user_email";

    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Constructor que recibe el HttpContextAccessor para acceder a la sesión.
    /// </summary>
    public AuthTokenService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    /// <summary>
    /// Almacena el token JWT y los datos principales del usuario en la sesión.
    /// </summary>
    /// <param name="token">Token JWT recibido.</param>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="rol">Rol del usuario.</param>
    public void StoreToken(string token, string email, string rol)
    {
        Session.SetString(TokenKey, token);
        Session.SetString(UserEmailKey, email);
        Session.SetString(UserRoleKey, rol);

        // Decodifica el payload manualmente (base64url) para extraer claims sin librería JWT
        var parts = token.Split('.');
        if (parts.Length == 3)
        {
            try
            {
                var payload = parts[1];
                // Añade padding
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var id = TryGet(root, "sub", "nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                var name = TryGet(root, "name", "unique_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name") ?? email;

                if (id != null) Session.SetString(UserIdKey, id);
                Session.SetString(UserNameKey, name);
            }
            catch { /* usa el email como nombre por defecto */ }
        }

        if (Session.GetString(UserNameKey) == null)
            Session.SetString(UserNameKey, email);
    }

    private static string? TryGet(JsonElement root, params string[] keys)
    {
        foreach (var key in keys)
            if (root.TryGetProperty(key, out var val))
                return val.GetString();
        return null;
    }

    public string? GetToken() => Session.GetString(TokenKey);
    public string? GetUserId() => Session.GetString(UserIdKey);
    public string? GetUserName() => Session.GetString(UserNameKey);
    public string? GetUserRole() => Session.GetString(UserRoleKey);
    public string? GetUserEmail() => Session.GetString(UserEmailKey);
    public bool IsAuthenticated() => GetToken() is not null;

    public void Clear() => Session.Clear();
}
