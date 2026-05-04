namespace PersonalFinanceCli.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}