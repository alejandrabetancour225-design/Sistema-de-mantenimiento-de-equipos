namespace Backend.API.DTOs;

public class DashboardHighRiskResponse
{
    public Guid EquipmentId { get; set; }
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskComponent { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
