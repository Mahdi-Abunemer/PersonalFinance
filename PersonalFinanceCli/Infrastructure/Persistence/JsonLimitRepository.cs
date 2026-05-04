using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonLimitRepository : ILimitRepository
{
    private readonly JsonDataStore _store;

    public JsonLimitRepository(JsonDataStore store)
    {
        _store = store;
    }

    public DailyLimit? GetByDate(DateOnly date)
    {
        return _store.Load().DailyLimits.FirstOrDefault(limit => limit.Date == date);
    }

    public DailyLimit Upsert(DateOnly date, decimal amount, Currency currency)
    {
        var dataStore = _store.Load();

        var existingDailyLimit = dataStore.DailyLimits
            .FirstOrDefault(limit => limit.Date == date);
        if (existingDailyLimit is null)
        {
            existingDailyLimit = new DailyLimit
            {
                Id = dataStore.DailyLimits.Count == 0 
                ? 1
                : dataStore.DailyLimits.Max(x => x.Id) + 1,
                Date = date,
                Amount = amount,
                Currency = currency
            };
            dataStore.DailyLimits.Add(existingDailyLimit);
        }
        else
        {
            existingDailyLimit.Amount = amount;
            existingDailyLimit.Currency = currency;
        }

        _store.Save(dataStore);
        return existingDailyLimit;
    }
}
