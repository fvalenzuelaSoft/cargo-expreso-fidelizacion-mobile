namespace CargoExpreso.API.Interfaces;

public interface IConfigurationService
{
    Task<decimal> GetDecimalAsync(string key);
    Task<int>     GetIntAsync(string key);
    Task<string>  GetStringAsync(string key);
    Task<bool>    GetBoolAsync(string key);
    void          InvalidateCache(string key);
}
