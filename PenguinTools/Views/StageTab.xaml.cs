using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class StageTab : UserControl
{
    public StageTab(StageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
