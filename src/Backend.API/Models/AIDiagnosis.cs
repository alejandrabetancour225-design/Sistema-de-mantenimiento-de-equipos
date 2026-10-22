namespace Backend.API.Models;

public class AIDiagnosis
{
    public Guid Id { get; set; }

    public Guid EquipmentId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid CreatedByUserId { get; set; }

    public string Symptom { get; set; } = string.Empty;

    public string RankedCausesJson { get; set; } = "[]";

    public bool RequiresTechnician { get; set; }
    public string Reasoning { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAt { get; set; }

    public string ContextSnapshotJson { get; set; } = "{}";

    // Navegaciones
    public Equipment? Equipment { get; set; }
    public Incident? Incident { get; set; }
    public User? CreatedByUser { get; set; }
}
