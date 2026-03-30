using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedSync_Frontend.Services;

/// <summary>
/// Cliente HTTP para consumir la API de MedSync, maneja autenticación y serialización.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenService _auth;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Constructor que recibe la factoría de HttpClient y el servicio de autenticación.
    /// </summary>
    public ApiClient(IHttpClientFactory factory, AuthTokenService auth)
    {
        _http = factory.CreateClient("MedSyncApi");
        _auth = auth;
    }

    private void AddAuth()
    {
        var token = _auth.GetToken();
        if (token is not null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Realiza una petición GET autenticada a la API.
    /// </summary>
    /// <typeparam name="T">Tipo de dato esperado en la respuesta.</typeparam>
    /// <param name="url">Ruta relativa del endpoint.</param>
    /// <returns>Resultado de la API deserializado.</returns>
    public async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        AddAuth();
        var response = await _http.GetAsync(url);
        return await Parse<T>(response);
    }

    /// <summary>
    /// Realiza una petición POST autenticada a la API.
    /// </summary>
    /// <typeparam name="T">Tipo de dato esperado en la respuesta.</typeparam>
    /// <param name="url">Ruta relativa del endpoint.</param>
    /// <param name="body">Objeto a enviar como cuerpo.</param>
    /// <returns>Resultado de la API deserializado.</returns>
    public async Task<ApiResult<T>> PostAsync<T>(string url, object body)
    {
        AddAuth();
        var content = Serialize(body);
        var response = await _http.PostAsync(url, content);
        return await Parse<T>(response);
    }

    /// <summary>
    /// Realiza una petición PUT autenticada a la API.
    /// </summary>
    /// <typeparam name="T">Tipo de dato esperado en la respuesta.</typeparam>
    /// <param name="url">Ruta relativa del endpoint.</param>
    /// <param name="body">Objeto a enviar como cuerpo.</param>
    /// <returns>Resultado de la API deserializado.</returns>
    public async Task<ApiResult<T>> PutAsync<T>(string url, object body)
    {
        AddAuth();
        var content = Serialize(body);
        var response = await _http.PutAsync(url, content);
        return await Parse<T>(response);
    }

    /// <summary>
    /// Realiza una petición PATCH autenticada a la API.
    /// </summary>
    /// <typeparam name="T">Tipo de dato esperado en la respuesta.</typeparam>
    /// <param name="url">Ruta relativa del endpoint.</param>
    /// <param name="body">Objeto a enviar como cuerpo.</param>
    /// <returns>Resultado de la API deserializado.</returns>
    public async Task<ApiResult<T>> PatchAsync<T>(string url, object body)
    {
        AddAuth();
        var content = Serialize(body);
        var response = await _http.PatchAsync(url, content);
        return await Parse<T>(response);
    }

    public async Task<bool> DeleteAsync(string url)
    {
        AddAuth();
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    private static StringContent Serialize(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    private static async Task<ApiResult<T>> Parse<T>(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "null")
                return ApiResult<T>.Ok(default!);
            var data = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            return ApiResult<T>.Ok(data!);
        }

        // Try to extract error message from JSON body
        string error = raw;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                error = msg.GetString() ?? raw;
            else if (doc.RootElement.TryGetProperty("title", out var title))
                error = title.GetString() ?? raw;
        }
        catch { /* use raw */ }

        return ApiResult<T>.Fail(error, (int)response.StatusCode);
    }
}

public class ApiResult<T>
{
    public T? Data { get; private init; }
    public bool Success { get; private init; }
    public string? Error { get; private init; }
    public int StatusCode { get; private init; }

    public static ApiResult<T> Ok(T data) => new() { Data = data, Success = true };
    public static ApiResult<T> Fail(string error, int status) =>
        new() { Error = error, Success = false, StatusCode = status };
}
