using System.Globalization;
using System.Resources;

namespace PenguinTools.Infrastructure.Resources;

public class Strings
{
    private static ResourceManager? resourceMan;

    internal Strings()
    {
    }

    public static ResourceManager ResourceManager => resourceMan ??=
        new ResourceManager("PenguinTools.Infrastructure.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }
}