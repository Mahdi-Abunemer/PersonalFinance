using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class DataFile
{
    public List<Card> Cards { get; set; } = new();

    public List<Transaction> Transactions { get; set; } = new();

    public List<DailyLimit> DailyLimits { get; set; } = new();

    public DateOnly? LastCushionDeclinedDate { get; set; }

    public bool HasSeenOnboarding { get; set; }

    public Guid? DefaultCardId { get; set; }
}