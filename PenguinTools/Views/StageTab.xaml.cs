using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class StageTab : UserControl
{
    public StageTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<StageViewModel>();
    }
}