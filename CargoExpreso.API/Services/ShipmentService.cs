using CargoExpreso.API.Data;
using CargoExpreso.API.Domain;
using CargoExpreso.API.Domain.Entities;
using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Shipments;
using CargoExpreso.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CargoExpreso.API.Services;

public class ShipmentService : IShipmentService
{
    private readonly AppDbContext          _db;
    private readonly IConfigurationService _config;
    private readonly IAuditService         _audit;

    public ShipmentService(AppDbContext db, IConfigurationService config, IAuditService audit)
    {
        _db     = db;
        _config = config;
        _audit  = audit;
    }

    public async Task<ApiResponse<ShipmentScanResponse>> ScanAsync(
        ScanShipmentRequest request,
        Guid    customerId,
        string  identityNumber,
        string? ipAddress)
    {
        var tasa          = await _config.GetDecimalAsync("TASA_ACUMULACION_PUNTOS");
        var maxScansHour  = await _config.GetIntAsync("MAX_ESCANEOS_POR_HORA");

        // Fraud: rate-limit scans per customer per hour
        var scansLastHour = await _db.Shipments
            .CountAsync(s => s.ScannedByCustomerId == customerId
                          && s.ScannedAt >= DateTime.UtcNow.AddHours(-1));
        if (scansLastHour >= maxScansHour)
        {
            await _audit.LogAsync("Shipment", request.ShipmentNumber, "Scan", AuditResult.Rejected,
                customerId: customerId, rejectionReason: "RateLimitExceeded", ipAddress: ipAddress);
            return ApiResponse<ShipmentScanResponse>.Fail("Límite de escaneos por hora alcanzado");
        }

        // NOTE: In production, call CE Central API here first to fetch the shipment data.
        // If the shipment doesn't exist in our DB, create it from the CE response.
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.ShipmentNumber == request.ShipmentNumber);

        // Rule 1: Shipment must exist
        if (shipment is null)
        {
            await _audit.LogAsync("Shipment", request.ShipmentNumber, "Scan", AuditResult.Rejected,
                customerId: customerId, rejectionReason: "ShipmentNotFound", ipAddress: ipAddress);
            return ApiResponse<ShipmentScanResponse>.Fail("Guía no encontrada en el sistema");
        }

        // Rule 2: Scan window (72h)
        if (DateTime.UtcNow > shipment.ExpiresAt)
        {
            await _db.Shipments.Where(s => s.Id == shipment.Id && s.Status == ShipmentStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ShipmentStatus.Expired));

            await _audit.LogAsync("Shipment", shipment.Id.ToString(), "Scan", AuditResult.Rejected,
                customerId: customerId, rejectionReason: "ScanWindowExpired", ipAddress: ipAddress);
            return ApiResponse<ShipmentScanResponse>.Fail("La ventana de escaneo de 72 horas ha expirado");
        }

        // Rule 3: Only once
        if (shipment.Status == ShipmentStatus.Scanned)
        {
            await _audit.LogAsync("Shipment", shipment.Id.ToString(), "Scan", AuditResult.Rejected,
                customerId: customerId, rejectionReason: "AlreadyScanned", ipAddress: ipAddress);
            return ApiResponse<ShipmentScanResponse>.Fail("Esta guía ya fue escaneada anteriormente");
        }

        // Rule 4: Identity must match
        if (shipment.OwnerIdentityNumber != identityNumber)
        {
            await _audit.LogAsync("Shipment", shipment.Id.ToString(), "Scan", AuditResult.Rejected,
                customerId: customerId, rejectionReason: "IdentityMismatch", ipAddress: ipAddress);
            return ApiResponse<ShipmentScanResponse>.Fail("El número de identidad no coincide con la guía");
        }

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId);
        if (account is null)
            return ApiResponse<ShipmentScanResponse>.Fail("Cuenta de puntos no encontrada");

        var points        = Math.Round(shipment.ShipmentAmount * (tasa / 100), 2);
        var balanceBefore = account.Balance;
        var balanceAfter  = balanceBefore + points;

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Atomic mark — WHERE Status=Pending prevents race condition without RowVersion
            var marked = await _db.Shipments
                .Where(s => s.Id == shipment.Id && s.Status == ShipmentStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status,              ShipmentStatus.Scanned)
                    .SetProperty(x => x.ScannedAt,           DateTime.UtcNow)
                    .SetProperty(x => x.ScannedByCustomerId, customerId)
                    .SetProperty(x => x.PointsAwarded,       points));

            if (marked == 0)
            {
                await tx.RollbackAsync();
                return ApiResponse<ShipmentScanResponse>.Fail("Conflicto: la guía fue procesada simultáneamente");
            }

            account.Balance          = balanceAfter;
            account.TotalAccumulated += points;
            account.LastActivityAt   = DateTime.UtcNow;
            account.UpdatedAt        = DateTime.UtcNow;

            _db.PointsTransactions.Add(new PointsTransaction
            {
                PointsAccountId = account.Id,
                CustomerId      = customerId,
                TransactionType = TransactionType.Accumulation,
                Amount          = points,
                BalanceBefore   = balanceBefore,
                BalanceAfter    = balanceAfter,
                ShipmentId      = shipment.Id,
                IpAddress       = ipAddress,
                CreatedAt       = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.LogAsync("Shipment", shipment.Id.ToString(), "Scan", AuditResult.Success,
                customerId: customerId, ipAddress: ipAddress);

            return ApiResponse<ShipmentScanResponse>.Ok(new ShipmentScanResponse
            {
                ShipmentNumber = shipment.ShipmentNumber,
                ShipmentAmount = shipment.ShipmentAmount,
                PointsAwarded  = points,
                NewBalance     = balanceAfter,
                Message        = $"¡Ganaste L.{points:F2} en puntos!"
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
