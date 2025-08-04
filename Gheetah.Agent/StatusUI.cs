using Spectre.Console;

namespace Gheetah.Agent;

public class StatusUI
{
    public static void ShowStatus(string message)
    {
        string cleanMessage = message
            .Replace("[", "[[")
            .Replace("]", "]]") 
            .Replace("xUnit.net", "xUnit") 
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;");

        AnsiConsole.MarkupLine($"[green]Agent Status:[/] {cleanMessage}");
    }
}