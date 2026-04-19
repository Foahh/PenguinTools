using PenguinTools.Core.Asset;

namespace PenguinTools.Chart.Tests;

internal static class TestAssets
{
    public static AssetManager Load()
    {
        var dir = Path.GetDirectoryName(typeof(TestAssets).Assembly.Location)!;
        var cursor = dir;
        while (cursor is not null && !File.Exists(Path.Combine(cursor, "assets.json")))
            cursor = Directory.GetParent(cursor)?.FullName;
        if (cursor is null)
            throw new FileNotFoundException("assets.json not found above " + dir);
        using var fs = File.OpenRead(Path.Combine(cursor, "assets.json"));
        var userDir = Path.Combine(Path.GetTempPath(), "PenguinChartTests", "user-assets");
        Directory.CreateDirectory(userDir);
        return new AssetManager(fs, userDir);
    }
}