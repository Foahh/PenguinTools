using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class MusicTab : UserControl
{
    public MusicTab(MusicViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}