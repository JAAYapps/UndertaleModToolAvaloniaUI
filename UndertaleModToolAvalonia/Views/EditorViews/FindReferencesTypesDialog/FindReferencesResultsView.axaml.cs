using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Linq;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog;

namespace UndertaleModToolAvalonia.Views.EditorViews.FindReferencesTypesDialog;

public partial class FindReferencesResultsView : Window
{
    public FindReferencesResultsView()
    {
        InitializeComponent();

        DoneButton.Click += (s, e) => Close();
        ResultsTree.DoubleTapped += (s, e) =>
        {
            if (DataContext is FindReferencesResultsViewModel vm)
                vm.OpenCommand.Execute(vm.SelectedResult);
        };
        ResultsTree.AddHandler(KeyDownEvent, ResultsTree_KeyDown, RoutingStrategies.Tunnel);
        ResultsTree.AddHandler(PointerPressedEvent, ResultsTree_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is not FindReferencesResultsViewModel vm)
            return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var existing = desktop.Windows.OfType<FindReferencesResultsView>().LastOrDefault(x =>
        {
            return x != this && x.DataContext is FindReferencesResultsViewModel other && Equals(other.SourceObject, vm.SourceObject);
        });
        if (existing is not null)
        {
            Position = existing.Position;
            existing.Close();
        }
    }

    private void ResultsTree_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return)
            return;

        if (DataContext is FindReferencesResultsViewModel { SelectedResult.IsResultItem: true } vm)
        {
            vm.OpenCommand.Execute(vm.SelectedResult);
            e.Handled = true;
        }
    }

    private void ResultsTree_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ResultsTree).Properties.IsMiddleButtonPressed)
            return;

        if (DataContext is FindReferencesResultsViewModel vm
            && (e.Source as StyledElement)?.DataContext is ReferenceResultNode { IsResultItem: true } node)
        {
            vm.SelectedResult = node;
            vm.OpenInNewTabCommand.Execute(node);
        }
    }
}