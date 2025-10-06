using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;
using System.Windows.Controls;

namespace PenguinTools.Pages;

public partial class MiscTab : UserControl
{
    public MiscTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MiscViewModel>();
    }
}