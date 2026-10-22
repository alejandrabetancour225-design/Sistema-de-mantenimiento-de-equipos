namespace Backend.API.DTOs;

public enum ErrorKnowledgeActionStatus
{
    Success,
    NotFound,
    DuplicateSymptom,
    InvalidData
}

public record ErrorKnowledgeResult(ErrorKnowledgeActionStatus Status, ErrorKnowledgeResponse? Knowledge);
