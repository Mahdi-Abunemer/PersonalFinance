namespace PersonalFinanceCli.Infrastructure.Time;

public sealed class FakeClock : IClock
{
    public DateOnly Today { get; set; }

    public FakeClock(DateOnly today)
    {
        Today = today;
    }
}