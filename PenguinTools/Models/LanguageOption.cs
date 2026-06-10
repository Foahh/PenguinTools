namespace PenguinTools.Models;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public static readonly LanguageOption[] All =
    [
        new("en", "English"),
        new("zh-Hans", "简体中文")
    ];
}