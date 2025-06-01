using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;
using System.Windows.Controls;

namespace PenguinTools.Pages;

public partial class WorkflowTab : UserControl
{
    public WorkflowTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<WorkflowViewModel>();
    }
}