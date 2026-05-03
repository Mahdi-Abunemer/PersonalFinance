namespace PersonalFinanceCli.Presentation.Parsing;

public sealed record CardAddCommand(
    string Name, 
    string Currency, 
    decimal? InitialBalance) : ParsedCommand;