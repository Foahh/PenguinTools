using PenguinTools.Core.Asset;

namespace PenguinTools.Core.Chart.Parser;

public sealed record MgxcParseRequest(string Path, AssetManager Assets);
