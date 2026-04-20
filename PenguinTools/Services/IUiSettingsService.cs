using PenguinTools.Models;

namespace PenguinTools.Services;

public interface IUiSettingsService
{
    UiSettings Settings { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    string? GetOptionDirectory(string optionId);

    void SetOptionDirectory(string optionId, string directory);
}
