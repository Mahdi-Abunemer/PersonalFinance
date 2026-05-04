namespace PersonalFinanceCli.Presentation.Rendering;

public sealed class SystemConsole : IConsole
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void Write(string text)
    {
        Console.Write(text);
    }

    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }

    public void WriteLine()
    {
        Console.WriteLine();
    }

    public TextWriter Out => Console.Out;
}