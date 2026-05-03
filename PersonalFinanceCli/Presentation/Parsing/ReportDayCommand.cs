namespace PersonalFinanceCli.Presentation.Parsing;

public sealed record ReportDayCommand(DateOnly? Date) : ParsedCommand;