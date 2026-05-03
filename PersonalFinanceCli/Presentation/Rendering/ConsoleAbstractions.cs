namespace PersonalFinanceCli.Presentation.Rendering;

public interface IConsole
{
    string? ReadLine();

    void Write(string text);

    void WriteLine(string text);

    void WriteLine();

    TextWriter Out { get; }
}