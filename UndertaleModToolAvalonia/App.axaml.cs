using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Win32;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Utility;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;
using UndertaleModToolAvalonia.ViewModels.MainWindowViewModels;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews;
using UndertaleModToolAvalonia.Views.StartPageViews;
using MainWindow = UndertaleModToolAvalonia.Views.MainWindow;
using MainWindowViewModel = UndertaleModToolAvalonia.ViewModels.MainWindowViewModel;

namespace UndertaleModToolAvalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Program.completeFail)
            {
                Current.ShowError(Program.failMessage);
                return;
            }
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            // Window main = WindowLoader.createWindow(null,
            //     typeof(EditorView), typeof(EditorViewModel), true, false);
            
            try
            {
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = new MainWindowViewModel(),
                };
                // Window main = WindowLoader.createWindow(null, typeof(MainWindow), typeof(MainWindowViewModel), true, false);
                // desktop.MainWindow = main;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (desktop.MainWindow != null)
                    desktop.MainWindow.Close();
                Window main = WindowLoader.findWindow(typeof(MainWindow));
                if (main != null)
                {
                    main.Hide();
                    main.Loaded += (_, __) =>
                    {
                        main.Close();
                    };
                }
                Current.ShowError(e.ToString());
                Program.completeFail = true;
            }
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}