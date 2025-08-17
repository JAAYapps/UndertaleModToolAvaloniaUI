using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Windowing;
using System;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Views
{
    public partial class MainWindow : AppWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
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
            {
                // Now we can call the startup command and pass a reference to this window
                await vm.StartupCommand.ExecuteAsync(this);
            }

            MainContentGrid.Focus();
        }
    }
}
