using System.IO;
using System.Threading;
using PenguinTools.Core;
using PenguinTools.Models;

namespace PenguinTools.Services;

public sealed class UiSettingsService : IUiSettingsService
{
    private const string FileName = "ui-settings.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IApplicationPaths _paths;

    public UiSettingsService(IApplicationPaths paths)
    {
        _paths = paths;
    }

    public UiSettings Settings { get; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_paths.UserDataPath);
            await JsonPersistence.LoadIntoAsync(Settings, _paths.UserDataPath, FileName, cancellationToken);
            Normalize(Settings);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_paths.UserDataPath);
            Normalize(Settings);
            await JsonPersistence.SaveFromAsync(Settings, _paths.UserDataPath, FileName, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public string? GetOptionDirectory(string optionId)
    {
        if (string.IsNullOrWhiteSpace(optionId)) return null;

        return Settings.OptionDirectories.TryGetValue(optionId.Trim(), out var directory) &&
               !string.IsNullOrWhiteSpace(directory)
            ? directory
            : null;
    }

    public void SetOptionDirectory(string optionId, string directory)
    {
        if (string.IsNullOrWhiteSpace(optionId)) return;

        var normalizedDirectory = NormalizeDirectory(directory);
        if (string.IsNullOrWhiteSpace(normalizedDirectory))
        {
            Settings.OptionDirectories.Remove(optionId.Trim());
            return;
        }

        Settings.OptionDirectories[optionId.Trim()] = normalizedDirectory;
    }

    private static void Normalize(UiSettings settings)
    {
        settings.GameDirectory = NormalizeDirectory(settings.GameDirectory);

        settings.OptionDirectories = settings.OptionDirectories
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
            .Select(kv => new KeyValuePair<string, string>(kv.Key.Trim(), NormalizeDirectory(kv.Value)))
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectory(string? directory)
    {
        return string.IsNullOrWhiteSpace(directory)
            ? string.Empty
            : Path.TrimEndingDirectorySeparator(directory.Trim());
    }
}
