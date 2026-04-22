using PenguinTools.Core.Asset;

namespace PenguinTools.Chart.Parser.sus;

public sealed record SusParseRequest(string Path, AssetManager Assets);
