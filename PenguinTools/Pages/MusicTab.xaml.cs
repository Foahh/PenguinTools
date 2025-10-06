using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;
using System.Windows.Controls;

namespace PenguinTools.Pages;

public partial class MusicTab : UserControl
{
    public MusicTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MusicViewModel>();
    }
}