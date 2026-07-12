using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }
    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // We only want this to run once
        this.Loaded -= MainWindow_Loaded;
            
        if (DataContext is MainWindowViewModel vm)
            await vm.StartupCommand.ExecuteAsync(this);
    }
}