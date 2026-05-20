using CargoExpreso.API.Data;
using CargoExpreso.API.Domain;
using CargoExpreso.API.Domain.Entities;
using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Customers;
using CargoExpreso.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CargoExpreso.API.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext          _db;
    private readonly IConfigurationService _config;
    private readonly IAuditService         _audit;

    public CustomerService(AppDbContext db, IConfigurationService config, IAuditService audit)
    {
        _db     = db;
        _config = config;
        _audit  = audit;
    }

    public async Task<ApiResponse<CustomerResponse>> RegisterAsync(RegisterCustomerRequest request, string? ipAddress)
    {
        if (await _db.Customers.AnyAsync(c => c.IdentityNumber == request.IdentityNumber))
            return ApiResponse<CustomerResponse>.Fail("El número de identidad ya está registrado");

        if (await _db.Customers.AnyAsync(c => c.Phone == request.Phone))
            return ApiResponse<CustomerResponse>.Fail("El teléfono ya está registrado");

        // Determine profile completion level and bonus
        var level = DetermineLevel(request);
        var bonusKey = level switch
        {
            ProfileCompletionLevel.PhoneOnly => "BONUS_SOLO_TELEFONO",
            ProfileCompletionLevel.WithEmail => "BONUS_CON_CORREO",
            _                                => "BONUS_PERFIL_COMPLETO"
        };
        var bonus = await _config.GetDecimalAsync(bonusKey);

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var customer = new Customer
            {
                IdentityNumber = request.IdentityNumber,
                FirstName      = request.FirstName,
                LastName       = request.LastName,
                Phone          = request.Phone,
                DeviceToken    = request.DeviceToken,
                Status         = CustomerStatus.Active,
                CreatedAt      = DateTime.UtcNow,
                UpdatedAt      = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            var profile = new CustomerProfile
            {
                CustomerId              = customer.Id,
                Email                   = request.Email,
                Address                 = request.Address,
                BirthDate               = request.BirthDate,
                CountryId               = request.CountryId,
                ProfileCompletionLevel  = level,
                IsProfileComplete       = level == ProfileCompletionLevel.Complete,
                BonusApplied            = bonus,
                UpdatedAt               = DateTime.UtcNow
            };
            _db.CustomerProfiles.Add(profile);

            var account = new PointsAccount
            {
                CustomerId       = customer.Id,
                Balance          = bonus,
                TotalAccumulated = bonus,
                CreatedAt        = DateTime.UtcNow,
                UpdatedAt        = DateTime.UtcNow
            };
            _db.PointsAccounts.Add(account);
            await _db.SaveChangesAsync();

            // Register bonus transaction if > 0
            if (bonus > 0)
            {
                _db.PointsTransactions.Add(new PointsTransaction
                {
                    PointsAccountId = account.Id,
                    CustomerId      = customer.Id,
                    TransactionType = TransactionType.RegistrationBonus,
                    Amount          = bonus,
                    BalanceBefore   = 0,
                    BalanceAfter    = bonus,
                    Notes           = $"Bonus de registro — nivel {level}",
                    IpAddress       = ipAddress,
                    CreatedAt       = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            await _audit.LogAsync("Customer", customer.Id.ToString(), "Register", AuditResult.Success,
                customerId: customer.Id, ipAddress: ipAddress);

            return ApiResponse<CustomerResponse>.Ok(MapToResponse(customer, profile, account));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<CustomerResponse>> GetProfileAsync(Guid customerId)
    {
        var customer = await _db.Customers
            .Include(c => c.Profile)
            .Include(c => c.PointsAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        return customer is null
            ? ApiResponse<CustomerResponse>.Fail("Cliente no encontrado")
            : ApiResponse<CustomerResponse>.Ok(MapToResponse(customer, customer.Profile, customer.PointsAccount));
    }

    public async Task<ApiResponse<CustomerResponse>> UpdateProfileAsync(Guid customerId, UpdateProfileRequest request, string? ipAddress)
    {
        var customer = await _db.Customers
            .Include(c => c.Profile)
            .Include(c => c.PointsAccount)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer is null)
            return ApiResponse<CustomerResponse>.Fail("Cliente no encontrado");

        var profile  = customer.Profile!;
        var prevLevel = profile.ProfileCompletionLevel;

        // Apply updates
        if (!string.IsNullOrEmpty(request.Email))    profile.Email     = request.Email;
        if (!string.IsNullOrEmpty(request.Address))  profile.Address   = request.Address;
        if (request.BirthDate.HasValue)              profile.BirthDate = request.BirthDate;
        if (request.CountryId.HasValue)              profile.CountryId = request.CountryId;
        if (!string.IsNullOrEmpty(request.DeviceToken)) customer.DeviceToken = request.DeviceToken;

        var newLevel = DetermineLevel(customer, profile);
        profile.ProfileCompletionLevel = newLevel;
        profile.IsProfileComplete      = newLevel == ProfileCompletionLevel.Complete;
        profile.UpdatedAt              = DateTime.UtcNow;
        customer.UpdatedAt             = DateTime.UtcNow;

        // Apply incremental bonus if profile level improved
        if (newLevel > prevLevel)
        {
            var bonusKey = newLevel switch
            {
                ProfileCompletionLevel.WithEmail => "BONUS_CON_CORREO",
                _                                => "BONUS_PERFIL_COMPLETO"
            };
            var additionalBonus = await _config.GetDecimalAsync(bonusKey) - profile.BonusApplied;
            if (additionalBonus > 0)
            {
                var account = customer.PointsAccount!;
                var before  = account.Balance;
                account.Balance          += additionalBonus;
                account.TotalAccumulated += additionalBonus;
                account.UpdatedAt         = DateTime.UtcNow;
                profile.BonusApplied     += additionalBonus;

                _db.PointsTransactions.Add(new PointsTransaction
                {
                    PointsAccountId = account.Id,
                    CustomerId      = customerId,
                    TransactionType = TransactionType.RegistrationBonus,
                    Amount          = additionalBonus,
                    BalanceBefore   = before,
                    BalanceAfter    = account.Balance,
                    Notes           = $"Bonus adicional por completar perfil — nivel {newLevel}",
                    IpAddress       = ipAddress,
                    CreatedAt       = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("Customer", customerId.ToString(), "UpdateProfile", AuditResult.Success,
            customerId: customerId, ipAddress: ipAddress);

        return ApiResponse<CustomerResponse>.Ok(MapToResponse(customer, profile, customer.PointsAccount));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProfileCompletionLevel DetermineLevel(RegisterCustomerRequest r)
    {
        bool hasEmail   = !string.IsNullOrEmpty(r.Email);
        bool hasAddress = !string.IsNullOrEmpty(r.Address);
        bool hasBirth   = r.BirthDate.HasValue;

        if (hasEmail && hasAddress && hasBirth) return ProfileCompletionLevel.Complete;
        if (hasEmail)                            return ProfileCompletionLevel.WithEmail;
        return ProfileCompletionLevel.PhoneOnly;
    }

    private static ProfileCompletionLevel DetermineLevel(Customer c, CustomerProfile p)
    {
        bool hasEmail   = !string.IsNullOrEmpty(p.Email);
        bool hasAddress = !string.IsNullOrEmpty(p.Address);
        bool hasBirth   = p.BirthDate.HasValue;

        if (hasEmail && hasAddress && hasBirth) return ProfileCompletionLevel.Complete;
        if (hasEmail)                            return ProfileCompletionLevel.WithEmail;
        return ProfileCompletionLevel.PhoneOnly;
    }

    private static CustomerResponse MapToResponse(Customer c, CustomerProfile? p, PointsAccount? a) => new()
    {
        Id               = c.Id,
        IdentityNumber   = c.IdentityNumber,
        FullName         = $"{c.FirstName} {c.LastName}",
        Phone            = c.Phone,
        Email            = p?.Email,
        Status           = c.Status.ToString(),
        Balance          = a?.Balance ?? 0,
        TotalAccumulated = a?.TotalAccumulated ?? 0,
        TotalRedeemed    = a?.TotalRedeemed ?? 0,
        ProfileLevel     = p?.ProfileCompletionLevel.ToString() ?? "PhoneOnly",
        IsProfileComplete = p?.IsProfileComplete ?? false,
        CreatedAt        = c.CreatedAt
    };
}
