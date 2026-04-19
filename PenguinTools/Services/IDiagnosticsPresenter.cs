using PenguinTools.Core;

namespace PenguinTools.Services;

public interface IDiagnosticsPresenter
{
    void Show(DiagnosticSnapshot snapshot);
}