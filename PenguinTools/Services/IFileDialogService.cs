namespace PenguinTools.Services;

public interface IFileDialogService
{
    Task<string?> PickFolderAsync(string title, string? initialDirectory, Guid? clientGuid = null);
}
