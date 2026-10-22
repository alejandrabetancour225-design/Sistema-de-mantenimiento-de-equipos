namespace Backend.API.Models;

public class FailurePrediction
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }

    public int RiskScore { get; set; }

    public string RiskComponent { get; set; } = "General";

    public string Reason { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = "v1";

    public bool Acknowledged { get; set; }

    public DateTime PredictedAt { get; set; }

    // Navegación
    public Equipment? Equipment { get; set; }
}
