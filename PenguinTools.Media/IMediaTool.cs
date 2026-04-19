namespace PenguinTools.Media;

public interface IMediaTool
{
    Task<ProcessCommandResult> NormalizeAudioAsync(string src, string dst, decimal offset, CancellationToken ct = default);

    Task<ProcessCommandResult> CheckAudioValidAsync(string src, CancellationToken ct = default);

    Task<ProcessCommandResult> CheckImageValidAsync(string src, CancellationToken ct = default);

    Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default);

    Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths, CancellationToken ct = default);

    Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default);
}
