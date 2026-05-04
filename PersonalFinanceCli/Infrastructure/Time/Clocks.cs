namespace PersonalFinanceCli.Infrastructure.Time;

public interface IClock
{
    DateOnly Today { get; }
}
