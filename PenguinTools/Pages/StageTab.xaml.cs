using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;
using System.Windows.Controls;

namespace PenguinTools.Pages;

public partial class StageTab : UserControl
{
    public StageTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<StageViewModel>();
    }
}