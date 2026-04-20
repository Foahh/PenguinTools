using PenguinTools.Core.Diagnostic;

namespace PenguinTools.Services;

public interface IDiagnosticsPresenter
{
    void Show(DiagnosticSnapshot snapshot);
}