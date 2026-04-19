using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace PenguinTools.Behaviors;

public class TreeViewCollapseSiblingsBehavior : Behavior<TreeView>
{
    protected override void OnAttached()
    {
        AssociatedObject.AddHandler(TreeViewItem.ExpandedEvent, OnTreeViewItemExpanded, true);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.RemoveHandler(TreeViewItem.ExpandedEvent, OnTreeViewItemExpanded);
    }

    private static void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem expandedItem) return;
        var parent = ItemsControl.ItemsControlFromItemContainer(expandedItem);
        if (parent == null) return;
        foreach (var sibling in parent.Items)
        {
            var sibContainer = (TreeViewItem?)parent.ItemContainerGenerator.ContainerFromItem(sibling);
            if (sibContainer != null && sibContainer != expandedItem) sibContainer.IsExpanded = false;
        }

        e.Handled = true;
    }
}
