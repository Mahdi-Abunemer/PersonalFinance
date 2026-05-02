using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.Repositories;

public interface ILimitRepository
{
    DailyLimit? GetByDate(DateOnly date);

    DailyLimit Upsert(DateOnly date, decimal amount, Currency currency);
}
