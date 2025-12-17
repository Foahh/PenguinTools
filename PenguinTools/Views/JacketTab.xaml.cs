using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class JacketTab : UserControl
{
    public JacketTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<JacketViewModel>();
    }
}