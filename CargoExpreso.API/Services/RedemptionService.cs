using CargoExpreso.API.Data;
using CargoExpreso.API.Domain;
using CargoExpreso.API.Domain.Entities;
using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Redemptions;
using CargoExpreso.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace CargoExpreso.API.Services;

public class RedemptionService : IRedemptionService
{
    private readonly AppDbContext          _db;
    private readonly IConfigurationService _config;
    private readonly IAuditService         _audit;

    public RedemptionService(AppDbContext db, IConfigurationService config, IAuditService audit)
    {
        _db     = db;
        _config = config;
        _audit  = audit;
    }

    public async Task<ApiResponse<RedemptionResponse>> CreateAsync(CreateRedemptionRequest request, Guid customerId)
    {
        var minBalance     = await _config.GetDecimalAsync("SALDO_MINIMO_CANJE");
        var qrMinutes      = await _config.GetIntAsync("VIGENCIA_QR_MINUTOS");
        var maxActiveQrs   = await _config.GetIntAsync("MAX_QR_ACTIVOS_SIMULTANEOS");

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId);
        if (account is null)
            return ApiResponse<RedemptionResponse>.Fail("Cuenta de puntos no encontrada");

        if (account.Balance < minBalance)
            return ApiResponse<RedemptionResponse>.Fail($"Saldo mínimo requerido: L.{minBalance:F2}");

        if (request.Amount > account.Balance)
            return ApiResponse<RedemptionResponse>.Fail("El monto supera el saldo disponible");

        if (request.Amount <= 0)
            return ApiResponse<RedemptionResponse>.Fail("El monto de canje debe ser mayor a cero");

        // Fraud: max active QR codes
        var activeQrs = await _db.RedemptionQrCodes
            .CountAsync(q => q.CustomerId == customerId && !q.IsUsed && q.ExpiresAt > DateTime.UtcNow);
        if (activeQrs >= maxActiveQrs)
            return ApiResponse<RedemptionResponse>.Fail($"Tiene {activeQrs} QR(s) activo(s). Espere a que expiren.");

        var qrValue  = Guid.NewGuid();
        var now      = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(qrMinutes);

        var redemptionRequest = new RedemptionRequest
        {
            CustomerId      = customerId,
            RequestedAmount = request.Amount,
            Status          = RedemptionRequestStatus.QrGenerated,
            RequestedAt     = now
        };
        _db.RedemptionRequests.Add(redemptionRequest);
        await _db.SaveChangesAsync();

        var qrCode = new RedemptionQrCode
        {
            RedemptionRequestId = redemptionRequest.Id,
            CustomerId          = customerId,
            QrCode              = qrValue,
            Amount              = request.Amount,
            GeneratedAt         = now,
            ExpiresAt           = expiresAt
        };
        _db.RedemptionQrCodes.Add(qrCode);
        await _db.SaveChangesAsync();

        var qrBase64 = GenerateQrBase64(qrValue.ToString());

        return ApiResponse<RedemptionResponse>.Ok(new RedemptionResponse
        {
            RequestId        = redemptionRequest.Id,
            QrCodeValue      = qrValue,
            QrCodeBase64     = qrBase64,
            Amount           = request.Amount,
            ExpiresAt        = expiresAt,
            RemainingBalance = account.Balance,
            Status           = RedemptionRequestStatus.QrGenerated.ToString()
        });
    }

    public async Task<ApiResponse<RedemptionResponse>> ApplyAsync(ApplyRedemptionRequest request, Guid operatorUserId, string? ipAddress)
    {
        var qr = await _db.RedemptionQrCodes
            .Include(q => q.RedemptionRequest)
            .FirstOrDefaultAsync(q => q.QrCode == request.QrCode && !q.IsUsed);

        if (qr is null)
        {
            await _audit.LogAsync("QrCode", request.QrCode.ToString(), "Redemption", AuditResult.Rejected,
                rejectionReason: "QrInvalidOrUsed", ipAddress: ipAddress);
            return ApiResponse<RedemptionResponse>.Fail("QR inválido o ya utilizado");
        }

        // Rule: QR expiry (30 min)
        if (DateTime.UtcNow > qr.ExpiresAt)
        {
            qr.IsUsed  = true;
            qr.UsedAt  = DateTime.UtcNow;
            qr.RedemptionRequest.Status      = RedemptionRequestStatus.Expired;
            qr.RedemptionRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync("QrCode", qr.Id.ToString(), "Redemption", AuditResult.Rejected,
                customerId: qr.CustomerId, rejectionReason: "QrExpired", ipAddress: ipAddress);
            return ApiResponse<RedemptionResponse>.Fail("El QR ha expirado (vigencia de 30 minutos superada)");
        }

        var account = await _db.PointsAccounts.FirstOrDefaultAsync(a => a.CustomerId == qr.CustomerId);
        if (account is null || account.Balance < qr.Amount)
            return ApiResponse<RedemptionResponse>.Fail("Saldo insuficiente para completar el canje");

        var balanceBefore = account.Balance;
        var balanceAfter  = balanceBefore - qr.Amount;

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Atomic mark — WHERE IsUsed=false prevents double-use race condition
            var marked = await _db.RedemptionQrCodes
                .Where(q => q.Id == qr.Id && !q.IsUsed)
                .ExecuteUpdateAsync(q => q
                    .SetProperty(x => x.IsUsed,           true)
                    .SetProperty(x => x.UsedAt,           DateTime.UtcNow)
                    .SetProperty(x => x.UsedByBranchId,   request.BranchId)
                    .SetProperty(x => x.UsedByOperatorId, operatorUserId));

            if (marked == 0)
            {
                await tx.RollbackAsync();
                return ApiResponse<RedemptionResponse>.Fail("QR ya fue procesado por otro terminal");
            }

            account.Balance        = balanceAfter;
            account.TotalRedeemed += qr.Amount;
            account.LastActivityAt = DateTime.UtcNow;
            account.UpdatedAt      = DateTime.UtcNow;

            qr.RedemptionRequest.Status          = RedemptionRequestStatus.Applied;
            qr.RedemptionRequest.CompletedAt     = DateTime.UtcNow;
            qr.RedemptionRequest.BranchId        = request.BranchId;
            qr.RedemptionRequest.OperatorUserId  = operatorUserId;

            _db.PointsTransactions.Add(new PointsTransaction
            {
                PointsAccountId    = account.Id,
                CustomerId         = qr.CustomerId,
                TransactionType    = TransactionType.Redemption,
                Amount             = -qr.Amount,
                BalanceBefore      = balanceBefore,
                BalanceAfter       = balanceAfter,
                RedemptionQrCodeId = qr.Id,
                BranchId           = request.BranchId,
                OperatorUserId     = operatorUserId,
                IpAddress          = ipAddress,
                CreatedAt          = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.LogAsync("QrCode", qr.Id.ToString(), "Redemption", AuditResult.Success,
                customerId: qr.CustomerId, userId: operatorUserId, branchId: request.BranchId, ipAddress: ipAddress);

            return ApiResponse<RedemptionResponse>.Ok(new RedemptionResponse
            {
                RequestId        = qr.RedemptionRequestId,
                Amount           = qr.Amount,
                RemainingBalance = balanceAfter,
                Status           = RedemptionRequestStatus.Applied.ToString()
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string GenerateQrBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData  = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode  = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(10);
        return Convert.ToBase64String(pngBytes);
    }
}
