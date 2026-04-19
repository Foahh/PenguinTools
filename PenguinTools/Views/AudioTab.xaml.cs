using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class AudioTab : UserControl
{
    public AudioTab(AudioViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
