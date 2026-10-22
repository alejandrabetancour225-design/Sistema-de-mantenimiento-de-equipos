namespace Backend.API.DTOs;

public class FailurePredictionResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public string EquipmentBrand { get; set; } = string.Empty;
    public string EquipmentModel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RiskComponent { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public bool Acknowledged { get; set; }
    public DateTime PredictedAt { get; set; }
}
