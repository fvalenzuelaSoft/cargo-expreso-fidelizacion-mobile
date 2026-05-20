using CargoExpreso.API.Data;
using CargoExpreso.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CargoExpreso.API.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly AppDbContext  _db;
    private readonly IMemoryCache  _cache;
    private const    int           TtlMinutes = 5;

    public ConfigurationService(AppDbContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    public async Task<decimal> GetDecimalAsync(string key) =>
        decimal.Parse(await GetRawAsync(key));

    public async Task<int> GetIntAsync(string key) =>
        int.Parse(await GetRawAsync(key));

    public async Task<string> GetStringAsync(string key) =>
        await GetRawAsync(key);

    public async Task<bool> GetBoolAsync(string key) =>
        bool.Parse(await GetRawAsync(key));

    public void InvalidateCache(string key) =>
        _cache.Remove(CacheKey(key));

    private async Task<string> GetRawAsync(string key)
    {
        var cacheKey = CacheKey(key);
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached!;

        var config = await _db.SystemConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive)
            ?? throw new InvalidOperationException($"System configuration '{key}' not found or inactive.");

        _cache.Set(cacheKey, config.ConfigValue, TimeSpan.FromMinutes(TtlMinutes));
        return config.ConfigValue;
    }

    private static string CacheKey(string key) => $"sysconfig_{key}";
}
