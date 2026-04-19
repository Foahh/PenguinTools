using System.Windows;
using System.Windows.Controls;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class OptionTab : UserControl
{
    public OptionTab(OptionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is OptionViewModel vm) vm.ApplyPreviewTreeSelection(e.NewValue);
    }
}