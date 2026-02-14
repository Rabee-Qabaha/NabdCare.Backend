namespace NabdCare.Application.Interfaces.Configuration;

public interface IExchangeRateService
{
    Task<decimal> GetRateAsync(string baseCurrency, string targetCurrency);
    Task UpdateRatesAsync();
}