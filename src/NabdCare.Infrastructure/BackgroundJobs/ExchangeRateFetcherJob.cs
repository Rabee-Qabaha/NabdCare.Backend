using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NabdCare.Domain.Entities.Configuration;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.BackgroundJobs;

public class ExchangeRateFetcherJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<ExchangeRateFetcherJob> _logger;
    private readonly IConfiguration _configuration;

    public ExchangeRateFetcherJob(
        IHttpClientFactory httpClientFactory,
        NabdCareDbContext dbContext,
        ILogger<ExchangeRateFetcherJob> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting exchange rate fetch job.");

        var apiKey = _configuration["ExchangeRateApi:ApiKey"];
        var baseUrl = _configuration["ExchangeRateApi:BaseUrl"];
        var baseCurrency = Currency.ILS; // Our system's base currency

        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
        {
            _logger.LogWarning("ExchangeRateApi:ApiKey is not configured. Skipping job.");
            return;
        }

        var client = _httpClientFactory.CreateClient();
        var url = $"{baseUrl}{apiKey}/latest/{baseCurrency}";

        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ExchangeRateApiResponse>(content);

            if (apiResponse?.conversion_rates == null)
            {
                _logger.LogWarning("Received null conversion rates from API.");
                return;
            }

            var now = DateTime.UtcNow;
            var supportedCurrencies = Enum.GetNames<Currency>();

            foreach (var currencyName in supportedCurrencies)
            {
                if (!apiResponse.conversion_rates.TryGetValue(currencyName, out var rateValue))
                {
                    _logger.LogWarning("Rate for currency '{Currency}' not found in API response.", currencyName);
                    continue;
                }

                var existingRate = await _dbContext.ExchangeRates
                    .FirstOrDefaultAsync(r => r.BaseCurrency == baseCurrency.ToString() && r.TargetCurrency == currencyName);

                if (existingRate != null)
                {
                    existingRate.Rate = rateValue;
                    existingRate.LastUpdated = now;
                }
                else
                {
                    _dbContext.ExchangeRates.Add(new ExchangeRate
                    {
                        BaseCurrency = baseCurrency.ToString(),
                        TargetCurrency = currencyName,
                        Rate = rateValue,
                        LastUpdated = now
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully updated exchange rates for supported currencies.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rates.");
        }
    }
}

public class ExchangeRateApiResponse
{
    public string? result { get; set; }
    public string? base_code { get; set; }
    public Dictionary<string, decimal>? conversion_rates { get; set; }
}