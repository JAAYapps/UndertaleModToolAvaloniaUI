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
        }
    }
}
