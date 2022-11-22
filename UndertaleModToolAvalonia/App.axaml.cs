using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;
using UndertaleModToolAvalonia.ViewModels.MainWindowViewModels;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews;

namespace UndertaleModToolAvalonia
{
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
                List<DataFileItem> dataFileItems = new List<DataFileItem>();
                DataFileItem OpenFile = new DataFileItem();
                OpenFile.Preview = "Open File";
                OpenFile.Name = "OpenFile";
                DataFileItem dataFile = new DataFileItem();
                dataFile.Preview = "Data.wim";
                dataFileItems.Add(OpenFile);
                dataFileItems.Add(dataFile);
                Window main = WindowLoader.createWindow(null,
                    typeof(MainWindow), typeof(MainWindowViewModel), true, false, dataFileItems);
                desktop.MainWindow = main;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
