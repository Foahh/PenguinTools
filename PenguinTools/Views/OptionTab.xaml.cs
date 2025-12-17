using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Core.Metadata;
using PenguinTools.Models;
using PenguinTools.ViewModels;

namespace PenguinTools.Views;

public partial class OptionTab : UserControl
{
    public OptionTab()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<OptionViewModel>();
    }

    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        var expandedItem = (TreeViewItem)sender;
        var parent = ItemsControl.ItemsControlFromItemContainer(expandedItem);
        if (parent == null) return;
        foreach (var sibling in parent.Items)
        {
            var sibContainer = (TreeViewItem)parent.ItemContainerGenerator.ContainerFromItem(sibling);
            if (sibContainer != null && sibContainer != expandedItem) sibContainer.IsExpanded = false;
        }
        e.Handled = true;
    }

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not OptionViewModel vm) return;
        if (e.NewValue is KeyValuePair<int, Book> kvpBook)
        {
            vm.SelectedBookItem = null;
            vm.SelectedBook = kvpBook.Value;
        }
        else if (e.NewValue is KeyValuePair<Difficulty, BookItem> kvpBookItem)
        {
            vm.SelectedBookItem = kvpBookItem.Value;
            vm.SelectedBook = null;
        }
        else
        {
            vm.SelectedBookItem = null;
            vm.SelectedBook = null;
        }
    }
}