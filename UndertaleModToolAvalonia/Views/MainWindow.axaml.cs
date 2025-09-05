using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Windowing;
using System;
using System.Runtime.InteropServices;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Views
{
    public partial class MainWindow : UndertaleWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            TitleBar.Height = 0;
            //TitleBar.ButtonHoverBackgroundColor = Color.Parse("#20FFFFFF");
            
            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
            this.Resized += MainWindow_Resized;
        }

        private void MainWindow_Resized(object? sender, WindowResizedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.IsMaximized = WindowState == WindowState.Maximized;
                vm.UpdateToggleIcon();
                Console.WriteLine("Maximize was clicked.");
            }
        }

        private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            // Handle Ctrl+O for Open File
            if (e.Key == Key.O && e.KeyModifiers == KeyModifiers.Control)
            {
                if (vm.OpenFileCommand.CanExecute(null))
                {
                    // Manually execute the command with the valid StorageProvider
                    vm.OpenFileCommand.Execute(this.StorageProvider);
                }
                e.Handled = true;
            }
            // Handle Ctrl+S for Save File
            else if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
            {
                if (vm.SaveFileCommand.CanExecute(null))
                {
                    vm.SaveFileCommand.Execute(this.StorageProvider);
                }
                e.Handled = true;
            }
            // Handle F5 for Run Game
            else if (e.Key == Key.F5)
            {
                if (vm.RunGameCommand.CanExecute(null))
                {
                    vm.RunGameCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // We only want this to run once
            this.Loaded -= MainWindow_Loaded;

            if (DataContext is MainWindowViewModel vm)
                await vm.StartupCommand.ExecuteAsync(this);

            MainContentGrid.Focus();
        }

        private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void Resize_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Get the edge from the Tag property of the Border
            if (sender is Border border && Enum.TryParse<WindowEdge>(border.Tag?.ToString(), out var edge))
            {
                // Start the resize drag operation
                this.BeginResizeDrag(edge, e);
            }
        }

        private void Maximize_OnClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            if (DataContext is MainWindowViewModel vm)
            {
                vm.IsMaximized = WindowState == WindowState.Maximized;
                vm.UpdateToggleIcon();
            }
        }

        private void Minimize_OnClick(object? sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CustomTitleBar_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            Maximize_OnClick(sender, e);
        }
    }
}
