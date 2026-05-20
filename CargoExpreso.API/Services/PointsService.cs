using CargoExpreso.API.Data;
using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Points;
using CargoExpreso.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CargoExpreso.API.Services;

public class PointsService : IPointsService
{
    private readonly AppDbContext _db;

    public PointsService(AppDbContext db) => _db = db;

    public async Task<ApiResponse<PointsAccountResponse>> GetBalanceAsync(Guid customerId)
    {
        var account = await _db.PointsAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == customerId);

        return account is null
            ? ApiResponse<PointsAccountResponse>.Fail("Cuenta no encontrada")
            : ApiResponse<PointsAccountResponse>.Ok(new PointsAccountResponse
            {
                AccountId        = account.Id,
                Balance          = account.Balance,
                TotalAccumulated = account.TotalAccumulated,
                TotalRedeemed    = account.TotalRedeemed,
                LastActivityAt   = account.LastActivityAt
            });
    }

    public async Task<ApiResponse<List<TransactionResponse>>> GetHistoryAsync(Guid customerId, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page     = Math.Max(page, 1);

        var transactions = await _db.PointsTransactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Shipment)
            .Include(t => t.Branch)
            .AsNoTracking()
            .Select(t => new TransactionResponse
            {
                Id             = t.Id,
                Type           = t.TransactionType.ToString(),
                Amount         = t.Amount,
                BalanceBefore  = t.BalanceBefore,
                BalanceAfter   = t.BalanceAfter,
                ShipmentNumber = t.Shipment != null ? t.Shipment.ShipmentNumber : null,
                BranchName     = t.Branch   != null ? t.Branch.Name             : null,
                Notes          = t.Notes,
                CreatedAt      = t.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<TransactionResponse>>.Ok(transactions);
    }
}
