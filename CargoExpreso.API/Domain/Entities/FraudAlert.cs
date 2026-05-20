using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class FraudAlert
{
    public Guid              Id                { get; set; }
    public Guid?             CustomerId        { get; set; }
    public string            AlertType         { get; set; } = default!;
    public FraudAlertSeverity Severity         { get; set; }
    public string            Description       { get; set; } = default!;
    public FraudAlertStatus  Status            { get; set; } = FraudAlertStatus.Open;
    public string?           RelatedEntityType { get; set; }
    public string?           RelatedEntityId   { get; set; }
    public DateTime          DetectedAt        { get; set; }
    public DateTime?         ResolvedAt        { get; set; }
    public Guid?             ResolvedByUserId  { get; set; }
    public string?           ResolutionNotes   { get; set; }

    public Customer? Customer        { get; set; }
    public User?     ResolvedByUser  { get; set; }
}
