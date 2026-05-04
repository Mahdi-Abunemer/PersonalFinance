namespace PersonalFinanceCli.Presentation.Parsing;

public sealed record LimitSetCommand(decimal Amount) : ParsedCommand;