using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class ChartTab : UserControl
{
    public ChartTab(ChartViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}