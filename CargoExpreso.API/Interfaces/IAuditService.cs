using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Interfaces;

public interface IAuditService
{
    Task LogAsync(
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
        object?     newValues       = null);
}
