using PenguinTools.Core.Metadata;

namespace PenguinTools.Media;

public sealed record AudioConvertRequest(
    Meta Meta,
    string OutFolder,
    string DummyAcbPath,
    string WorkingAudioPath,
    ulong HcaEncryptionKey = AudioConvertRequest.DefaultHcaEncryptionKey)
{
    public const ulong DefaultHcaEncryptionKey = 32931609366120192UL;
}