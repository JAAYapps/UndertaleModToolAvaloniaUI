using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.ViewModels.MainWindowViewModels.DataItemViewModels;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;
using Avalonia.Threading;
using System.Runtime;
using System.IO;
using UndertaleModLib.Models;
using UndertaleModLib;
using Uno.Client;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace UndertaleModToolAvalonia.ViewModels.MainWindowViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase content;
        private readonly Window perent;

        public MainWindowViewModel(Window perent, IEnumerable<DataFileItem> DataListItems)
        {
            this.perent = perent;
            LoadData = ReactiveCommand.Create<string, Task>(OpenDataAsync);
            Content = Item = new DataFileItemViewModel(DataListItems);
        }

        public ViewModelBase Content
        {
            get => content;
            private set => this.RaiseAndSetIfChanged(ref content, value);
        }

        public ReactiveCommand<string, Task> LoadData { get; }

        public async Task OpenDataAsync(string filename)
        {
            if (filename == "OpenFile")
            {
                try
                {
                    Window window = await WindowLoader.createWindowAsync(perent,
                        typeof(EditorView), typeof(EditorViewModel), true);
                    WindowLoader.setMainWindow(window);
                    EditorViewModel? editorViewModel = window.DataContext as EditorViewModel;
                    if (editorViewModel != null && await editorViewModel.OpenDialog())
                    {
                        window?.Show();
                        Window main = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
                        (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow = window;
                        main.Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to load file or Editor. ");
                        window?.Close();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error loading\n{e.Message}", perent);
                }
            }
        }

        public DataFileItemViewModel Item { get; }
    }
}
