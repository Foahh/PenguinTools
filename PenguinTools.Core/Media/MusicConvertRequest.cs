using PenguinTools.Core.Metadata;

namespace PenguinTools.Core.Media;

public sealed record MusicConvertRequest(Meta Meta, string OutFolder);
