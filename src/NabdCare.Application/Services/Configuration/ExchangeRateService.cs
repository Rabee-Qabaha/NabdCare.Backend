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
        if (rate != null)
        {
            return rate.Rate;
        }

        // Try to find the inverse rate
        var inverseRate = await _exchangeRateRepository.GetRateAsync(targetCurrency, baseCurrency);
        if (inverseRate != null && inverseRate.Rate > 0)
        {
            return 1 / inverseRate.Rate;
        }

        // Try cross-rate via System Base Currency (ILS)
        // If we want USD -> EUR, and we have ILS -> USD and ILS -> EUR
        // USD -> EUR = (ILS -> EUR) / (ILS -> USD)
        const string systemBase = "ILS";
        if (baseCurrency != systemBase && targetCurrency != systemBase)
        {
            var baseToSystemRate = await _exchangeRateRepository.GetRateAsync(systemBase, baseCurrency);
            var targetToSystemRate = await _exchangeRateRepository.GetRateAsync(systemBase, targetCurrency);

            if (baseToSystemRate != null && targetToSystemRate != null && baseToSystemRate.Rate > 0)
            {
                return targetToSystemRate.Rate / baseToSystemRate.Rate;
            }
        }

        throw new InvalidOperationException($"Exchange rate for {baseCurrency} to {targetCurrency} not found in the local cache. The background job may have failed.");
    }

    public Task UpdateRatesAsync()
    {
        // This is now handled by the ExchangeRateFetcherJob directly.
        // This method could be removed from the interface if it's not needed elsewhere.
        return Task.CompletedTask;
    }
}