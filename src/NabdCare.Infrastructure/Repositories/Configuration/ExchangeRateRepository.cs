using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Configuration;
using NabdCare.Domain.Entities.Configuration;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Configuration;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly NabdCareDbContext _dbContext;

    public ExchangeRateRepository(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExchangeRate?> GetRateAsync(string baseCurrency, string targetCurrency)
    {
        return await _dbContext.ExchangeRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == baseCurrency && r.TargetCurrency == targetCurrency)
            .OrderByDescending(r => r.LastUpdated)
            .FirstOrDefaultAsync();
    }

    public async Task<ExchangeRate?> FindRateAsync(string baseCurrency, string targetCurrency)
    {
        return await _dbContext.ExchangeRates
            .FirstOrDefaultAsync(r => r.BaseCurrency == baseCurrency && r.TargetCurrency == targetCurrency);
    }

    public void AddRate(ExchangeRate rate)
    {
        _dbContext.ExchangeRates.Add(rate);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}