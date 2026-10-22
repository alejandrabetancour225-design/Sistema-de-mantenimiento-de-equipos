using System.Net.Http.Json;
using System.Text.Json;
using Backend.API.DTOs;

namespace Backend.API.Services;

public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiClient> _logger;

    public AiClient(HttpClient httpClient, IConfiguration config, ILogger<AiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config["Ai:BaseUrl"] ?? "http://localhost:8000");
        _httpClient.Timeout = TimeSpan.FromSeconds(config.GetValue("Ai:TimeoutSeconds", 60));
        _logger = logger;
    }

    public async Task<FastApiDiagnosisResponse?> DiagnoseAsync(AiDiagnosisRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/diagnosis", request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("FastAPI devolvió {Status}: {Body}", response.StatusCode, body);
                return null;
            }

            return JsonSerializer.Deserialize<FastApiDiagnosisResponse>(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando al servicio de IA");
            return null;
        }
    }
}
