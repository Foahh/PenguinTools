using System.Globalization;
using System.Resources;

namespace PenguinTools.Core.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;
    private static CultureInfo? resourceCulture;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??= new ResourceManager("PenguinTools.Core.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture
    {
        get => resourceCulture;
        set => resourceCulture = value;
    }

    public static string Error_Song_id_is_not_set => ResourceManager.GetString(nameof(Error_Song_id_is_not_set), resourceCulture) ?? string.Empty;
    public static string Unit_Tick => ResourceManager.GetString(nameof(Unit_Tick), resourceCulture) ?? string.Empty;
}
