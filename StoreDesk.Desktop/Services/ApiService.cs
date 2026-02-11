using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace StoreDesk.Desktop.Services;

public class ApiService
{
    private const string BaseUrl = "http://127.0.0.1:5000/api";
    private static readonly string[] BaseUrlFallbacks = { "http://127.0.0.1:5000/api", "http://localhost:5000/api" };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private string _effectiveBaseUrl = BaseUrl;
    private bool _baseUrlProbed;

    public ApiService()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StoreDesk.Desktop/1.0");
    }

    private string Url(string endpoint)
    {
        if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return endpoint;
        return $"{_effectiveBaseUrl}/{endpoint.TrimStart('/')}";
    }

    private async Task<bool> TrySetBaseUrlAsync()
    {
        foreach (var baseUrl in BaseUrlFallbacks)
        {
            try
            {
                var r = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/Health");
                if (r.IsSuccessStatusCode) { _effectiveBaseUrl = baseUrl.TrimEnd('/'); return true; }
            }
            catch { /* пробуем следующий */ }
        }
        return false;
    }

    private async Task EnsureBaseUrlAsync()
    {
        if (_baseUrlProbed) return;
        _baseUrlProbed = true;
        await TrySetBaseUrlAsync();
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await EnsureBaseUrlAsync();
        try { var r = await _httpClient.GetAsync(Url(endpoint)); r.EnsureSuccessStatusCode(); return await r.Content.ReadFromJsonAsync<T>(JsonOptions); }
        catch (HttpRequestException) { throw; }
        catch (TaskCanceledException) { throw; }
        catch { return default; }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        await EnsureBaseUrlAsync();
        try
        {
            var r = await _httpClient.PostAsJsonAsync(Url(endpoint), data, JsonOptions);
            if (!r.IsSuccessStatusCode)
            {
                var body = await r.Content.ReadAsStringAsync();
                var msg = (r.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body))
                    ? (TryGetDetailFromJson(body) ?? body)
                    : $"{(int)r.StatusCode} {r.ReasonPhrase}";
                throw new HttpRequestException(msg);
            }
            return await r.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch (HttpRequestException) { throw; }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex) when (ex is not HttpRequestException) { return default; }
    }

    private static string? TryGetDetailFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("detail", out var d)) return d.GetString();
            if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
            if (doc.RootElement.TryGetProperty("title", out var t)) return t.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    public async Task<bool> PutAsync(string endpoint, object data)
    {
        await EnsureBaseUrlAsync();
        try { return (await _httpClient.PutAsJsonAsync(Url(endpoint), data, JsonOptions)).IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        await EnsureBaseUrlAsync();
        try { return (await _httpClient.DeleteAsync(Url(endpoint))).IsSuccessStatusCode; }
        catch { return false; }
    }
}
