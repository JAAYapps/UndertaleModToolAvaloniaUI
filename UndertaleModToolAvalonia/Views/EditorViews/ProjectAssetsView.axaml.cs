using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Linq;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Views.EditorViews;

public partial class ProjectAssetsView : Window
{
    public ProjectAssetsView()
    {
        InitializeComponent();

        AssetsGrid.DoubleTapped += (s, e) => OpenSelected(false);
        AssetsGrid.AddHandler(KeyDownEvent, AssetsGrid_KeyDown, RoutingStrategies.Tunnel);
        DropArea.AddHandler(DragDrop.DragOverEvent, DropArea_DragOver);
        DropArea.AddHandler(DragDrop.DropEvent, DropArea_Drop);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is not ProjectAssetsViewModel vm)
            return;
        vm.CloseRequested += ViewModel_CloseRequested;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var existing = desktop.Windows.OfType<ProjectAssetsView>().FirstOrDefault(x => x != this);
        if (existing is not null)
        {
            Position = existing.Position;
            existing.Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ProjectAssetsViewModel vm)
        {
            vm.CloseRequested -= ViewModel_CloseRequested;
            vm.Detach();
        }

        base.OnClosed(e);
    }

    private void ViewModel_CloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OpenSelected(bool inNewTab)
    {
        if (DataContext is not ProjectAssetsViewModel { SelectedAsset: UnexportedAsset asset } vm)
            return;

        if (inNewTab)
            vm.OpenInNewTabCommand.Execute(asset);
        else
            vm.OpenCommand.Execute(asset);
        ActivateMainWindow();
    }

    private void UnmarkSelected()
    {
        if (DataContext is ProjectAssetsViewModel vm)
            vm.UnmarkForExportCommand.Execute(AssetsGrid.SelectedItems);
    }

    private void AssetsGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            OpenSelected(false);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            UnmarkSelected();
            e.Handled = true;
        }
    }

    private void OpenMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        OpenSelected(false);
    }

    private void OpenInNewTabMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        OpenSelected(true);
    }

    private void UnmarkMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        UnmarkSelected();
    }

    private void DropArea_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = ProjectAssetsViewModel.CanMarkForExport(ResourceDragDrop.GetResource(e)) ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropArea_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is ProjectAssetsViewModel vm && vm.MarkForExport(ResourceDragDrop.GetResource(e)))
            e.DragEffects = DragDropEffects.Link;
        else
            e.DragEffects = DragDropEffects.None;

        e.Handled = true;
    }

    private static void ActivateMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow?.Activate();
    }
}
