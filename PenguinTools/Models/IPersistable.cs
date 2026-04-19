namespace PenguinTools.Models;

public interface IPersistable
{
    string PersistenceFileName { get; }

    Task LoadAsync(string directory, CancellationToken cancellationToken = default);

    Task SaveAsync(string directory, CancellationToken cancellationToken = default);
}
