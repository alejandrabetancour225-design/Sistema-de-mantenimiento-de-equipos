using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IErrorKnowledgeService
{
    Task<List<ErrorKnowledgeResponse>> GetAllAsync(string? componentType, string? symptom);
    Task<ErrorKnowledgeResponse?> GetByIdAsync(Guid id);
    Task<ErrorKnowledgeResult> CreateAsync(CreateErrorKnowledgeRequest request, Guid currentUserId);
    Task<ErrorKnowledgeResult> UpdateAsync(Guid id, UpdateErrorKnowledgeRequest request);
    Task<ErrorKnowledgeActionStatus> DeleteAsync(Guid id); // Soft delete
}
