using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Services.ProfileService;
using UndertaleModToolAvalonia.Services.UpdateService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;
using UndertaleModToolAvalonia.Views.EditorViews;
using MainWindow = UndertaleModToolAvalonia.Views.MainWindow;
using MainWindowViewModel = UndertaleModToolAvalonia.ViewModels.MainWindowViewModel;

namespace UndertaleModToolAvalonia;

public partial class App : Application
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    public static new App? Current => Application.Current as App;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Services = ConfigureServices();
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Program.completeFail)
            {
                _ = Current?.ShowError(Program.failMessage);
                return;
            }
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            // Window main = WindowLoader.createWindow(null,
            //     typeof(EditorView), typeof(EditorViewModel), true, false);

            

            try
            {
                desktop.MainWindow = desktop.MainWindow = Services.GetRequiredService<MainWindow>();

                WeakReferenceMessenger.Default.Register<SettingChangedMessage>(this, (recipient, message) =>
                {
                    if (message.SettingName == "EnableDarkMode" && message.NewValue is bool isDark)
                    {
                        // This one line changes the theme for the ENTIRE application.
                        // All open and future windows will automatically use the new theme.
                        Current.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
                    }
                    if (message.SettingName == nameof(SettingsPageViewModel.TransparencyGridColor1) && message.NewValue is string color1)
                    {
                        Application.Current!.Resources["TransparencyGridColor1"] = Color.Parse(color1);
                    }
                    else if (message.SettingName == nameof(SettingsPageViewModel.TransparencyGridColor2) && message.NewValue is string color2)
                    {
                        Application.Current!.Resources["TransparencyGridColor2"] = Color.Parse(color2);
                    }
                });
            }
            catch (Exception e)
            {
                // Avalonia is a bit weird when I only want to display a messagebox only to display a fatal error.
                // I had to create a new Window and hide it just for the framework to display the message.
                Console.WriteLine(e);
/*                if (desktop.MainWindow != null)
                    desktop.MainWindow.Close();
                Window main = new Window();
                if (main != null)
                {
                    main.Hide();
                    main.Loaded += (_, __) =>
                    {
                        main.Close();
                    };
                }*/
                _ = Current?.ShowError(e.ToString());
                Program.completeFail = true;
            }
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IPlayer, Player>();
        services.AddSingleton<IFileService, FileService>();
        services.AddTransient<LoaderDialogViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ProjectsPageViewModel>();
        services.AddTransient<DataFileViewModel>();
        services.AddSingleton<EditorViewModel>();
        services.AddTransient<SettingsPageViewModel>();

        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IDialogService, GMLSettingsDialogService>();
        services.AddSingleton<ILoadingDialogService, LoadingDialogService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        services.AddTransient<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainWindowViewModel>()
        });

        return services.BuildServiceProvider();
    }
}