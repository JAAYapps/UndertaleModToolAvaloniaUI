using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Linq;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Views.EditorViews;

public partial class SearchInCodeView : Window
{
    public SearchInCodeView()
    {
        InitializeComponent();

        SearchTextBox.AddHandler(KeyDownEvent, SearchTextBox_KeyDown, RoutingStrategies.Tunnel);
        ResultsGrid.DoubleTapped += (s, e) => OpenSelected(false);
        ResultsGrid.AddHandler(KeyDownEvent, ResultsGrid_KeyDown, RoutingStrategies.Tunnel);
        ResultsGrid.AddHandler(PointerPressedEvent, ResultsGrid_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        SearchTextBox.Focus();
        SearchTextBox.SelectAll();

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var existing = desktop.Windows.OfType<SearchInCodeView>().FirstOrDefault(x => x != this);
        if (existing is not null)
        {
            Position = existing.Position;
            Width = existing.Width;
            Height = existing.Height;
            existing.Close();
        }
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        e.Handled = true;
        if (DataContext is SearchInCodeViewModel vm && vm.SearchCommand.CanExecute(null))
            vm.SearchCommand.Execute(null);
    }

    private async void OpenSelected(bool inNewTab)
    {
        if (DataContext is not SearchInCodeViewModel vm || ResultsGrid.SelectedItems.Count == 0)
            return;

        await vm.OpenResults(ResultsGrid.SelectedItems, inNewTab);
        ActivateMainWindow();
    }

    private void ResultsGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return)
            return;

        OpenSelected(false);
        e.Handled = true;
    }

    private async void ResultsGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ResultsGrid).Properties.IsMiddleButtonPressed)
            return;

        if (DataContext is SearchInCodeViewModel vm
            && (e.Source as StyledElement)?.DataContext is SearchInCodeResult result)
        {
            await vm.OpenResults(new[] { result }, true);
            ActivateMainWindow();
        }
    }

    private static void ActivateMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow?.Activate();
    }
}