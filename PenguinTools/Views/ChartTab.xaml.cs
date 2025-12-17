using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class ChartTab : UserControl
{
    public ChartTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<ChartViewModel>();
    }
}