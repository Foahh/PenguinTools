using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class MusicTab : UserControl
{
    public MusicTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MusicViewModel>();
    }
}