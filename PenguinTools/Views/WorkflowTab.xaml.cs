using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class WorkflowTab : UserControl
{
    public WorkflowTab(WorkflowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
