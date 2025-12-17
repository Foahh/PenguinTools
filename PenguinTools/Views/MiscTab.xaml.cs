using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class MiscTab : UserControl
{
    public MiscTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MiscViewModel>();
    }
}