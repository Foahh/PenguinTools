using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class JacketTab : UserControl
{
    public JacketTab(JacketViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
