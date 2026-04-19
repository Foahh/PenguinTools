using PenguinTools.Core.Metadata;

namespace PenguinTools.Media;

public sealed record MusicConvertRequest(
    Meta Meta,
    string OutFolder,
    string DummyAcbPath,
    string WorkingAudioPath,
    ulong HcaEncryptionKey = MusicConvertRequest.DefaultHcaEncryptionKey)
{
    public const ulong DefaultHcaEncryptionKey = 32931609366120192UL;
}
