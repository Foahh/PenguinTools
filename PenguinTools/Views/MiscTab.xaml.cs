using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class MiscTab : UserControl
{
    public MiscTab(MiscViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}