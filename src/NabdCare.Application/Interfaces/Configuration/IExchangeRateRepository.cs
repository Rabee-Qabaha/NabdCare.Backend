using NabdCare.Domain.Entities.Configuration;

namespace NabdCare.Application.Interfaces.Configuration;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetRateAsync(string baseCurrency, string targetCurrency);
    Task<ExchangeRate?> FindRateAsync(string baseCurrency, string targetCurrency);
    void AddRate(ExchangeRate rate);
    Task<int> SaveChangesAsync();
}