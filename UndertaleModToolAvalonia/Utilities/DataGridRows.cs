using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Utilities
{
    public static class DataGridRows
    {
        public static readonly AttachedProperty<bool> CanUserDeleteRowsProperty =
            AvaloniaProperty.RegisterAttached<DataGrid, bool>("CanUserDeleteRows", typeof(DataGridRows));

        public static bool GetCanUserDeleteRows(DataGrid element) => element.GetValue(CanUserDeleteRowsProperty);
        public static void SetCanUserDeleteRows(DataGrid element, bool value) => element.SetValue(CanUserDeleteRowsProperty, value);

        static DataGridRows()
        {
            CanUserDeleteRowsProperty.Changed.AddClassHandler<DataGrid>((grid, args) =>
            {
                grid.RemoveHandler(InputElement.KeyDownEvent, DataGrid_KeyDown);
                if (GetCanUserDeleteRows(grid))
                    grid.AddHandler(InputElement.KeyDownEvent, DataGrid_KeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
            });
        }

        private static void DataGrid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete || sender is not DataGrid grid)
                return;
            if (e.Source is not Visual source || source.FindAncestorOfType<DataGrid>(includeSelf: true) != grid)
                return;
            if (source.FindAncestorOfType<TextBox>(includeSelf: true) is TextBox { IsReadOnly: false })
                return;
            if (grid.ItemsSource is not IList list || list.IsReadOnly)
                return;

            object? item = source.FindAncestorOfType<DataGridRow>()?.DataContext ?? grid.SelectedItem;
            if (item is null || !list.Contains(item))
                return;

            list.Remove(item);
            e.Handled = true;
        }
    }
}
