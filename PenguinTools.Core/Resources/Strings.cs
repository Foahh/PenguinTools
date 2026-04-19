using System.Globalization;
using System.Resources;

namespace PenguinTools.Core.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??=
        new ResourceManager("PenguinTools.Core.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string Error_Song_id_is_not_set =>
        ResourceManager.GetString(nameof(Error_Song_id_is_not_set), Culture) ?? string.Empty;

    public static string Unit_Tick => ResourceManager.GetString(nameof(Unit_Tick), Culture) ?? string.Empty;
}