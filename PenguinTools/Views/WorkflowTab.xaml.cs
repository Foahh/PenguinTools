using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class WorkflowTab : UserControl
{
    public WorkflowTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<WorkflowViewModel>();
    }
}