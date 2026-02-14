using NabdCare.Application.Interfaces.Configuration;

namespace NabdCare.Application.Services.Configuration;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;

    public ExchangeRateService(IExchangeRateRepository exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<decimal> GetRateAsync(string baseCurrency, string targetCurrency)
    {
        var rate = await _exchangeRateRepository.GetRateAsync(baseCurrency, targetCurrency);

        if (rate == null)
        {
            // In a real-world scenario, you might want to trigger an immediate fetch here
            // or return a default/stale rate. For now, throwing an exception is safest.
            throw new InvalidOperationException($"Exchange rate for {baseCurrency} to {targetCurrency} not found in the local cache. The background job may have failed.");
        }

        return rate.Rate;
    }

    public Task UpdateRatesAsync()
    {
        // This is now handled by the ExchangeRateFetcherJob directly.
        // This method could be removed from the interface if it's not needed elsewhere.
        return Task.CompletedTask;
    }
}