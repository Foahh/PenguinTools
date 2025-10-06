using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;
using System.Windows.Controls;

namespace PenguinTools.Pages;

public partial class JacketTab : UserControl
{
    public JacketTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<JacketViewModel>();
    }
}