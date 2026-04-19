using PenguinTools.Core.Asset;

namespace PenguinTools.Chart.Parser;

public sealed record MgxcParseRequest(string Path, AssetManager Assets);
