namespace PenguinTools.Models;

public sealed class UiSettings
{
    public Dictionary<string, string> OptionDirectories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GameDirectory { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
}
