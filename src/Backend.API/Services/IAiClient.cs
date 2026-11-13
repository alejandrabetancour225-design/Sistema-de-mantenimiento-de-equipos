using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IAiClient
{
    Task<FastApiDiagnosisResponse?> DiagnoseAsync(AiDiagnosisRequest request, CancellationToken ct = default);
    Task<AiTriageResponse?> TriageAsync(AiTriageRequest request, CancellationToken ct = default);
}
