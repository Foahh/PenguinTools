using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;
using System.Windows.Controls;

namespace PenguinTools.Pages;

public partial class ChartTab : UserControl
{
    public ChartTab()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ChartViewModel>();
    }
}