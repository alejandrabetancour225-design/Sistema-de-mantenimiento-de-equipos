using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IFeedbackService
{
    Task<FeedbackResult> CreateAsync(CreateFeedbackRequest request, Guid technicianId);
    Task<List<FeedbackResponse>> GetByDiagnosisAsync(Guid diagnosisId);
    Task<List<FeedbackResponse>> GetRecentAsync(int take = 20);
    Task<AIMetricsResponse> GetMetricsAsync();
}
