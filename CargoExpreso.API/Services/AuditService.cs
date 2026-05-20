using System.Text.Json;
using CargoExpreso.API.Data;
using CargoExpreso.API.Domain;
using CargoExpreso.API.Domain.Entities;
using CargoExpreso.API.Interfaces;

namespace CargoExpreso.API.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(
        string      entityType,
        string?     entityId,
        string      operationType,
        AuditResult result,
        Guid?       customerId      = null,
        Guid?       userId          = null,
        Guid?       branchId        = null,
        string?     rejectionReason = null,
        string?     ipAddress       = null,
        string?     userAgent       = null,
        object?     oldValues       = null,
        object?     newValues       = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            EntityType      = entityType,
            EntityId        = entityId,
            OperationType   = operationType,
            Result          = result,
            CustomerId      = customerId,
            UserId          = userId,
            BranchId        = branchId,
            RejectionReason = rejectionReason,
            IpAddress       = ipAddress,
            UserAgent       = userAgent,
            OldValues       = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues       = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CreatedAt       = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
