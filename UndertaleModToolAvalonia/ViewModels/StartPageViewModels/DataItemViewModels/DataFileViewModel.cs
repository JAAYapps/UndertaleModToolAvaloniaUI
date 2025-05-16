using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Utility;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

public partial class DataFileViewModel : ViewModelBase
{
    public EventHandler FileLoaded { get; set; }
    
    public DataFileViewModel()
    {
        List<DataFileItem> dataFileItems = new List<DataFileItem>();
        DataFileItem OpenFile = new DataFileItem();
        OpenFile.Preview = "Open File";
        OpenFile.Name = "OpenFile";
        DataFileItem dataFile = new DataFileItem();
        dataFile.Preview = "data.wim";
        dataFile.Name = "Somepath/data.wim";
        dataFileItems.Add(OpenFile);
        dataFileItems.Add(dataFile);
        foreach (var item in dataFileItems)
            Items.Add(new DataFileItemViewModel(item));
    }
    
    [RelayCommand]
    public async Task LoadData(string filename)
    {
        await Application.Current.ShowMessage($"Loading data from {filename}");
        if (filename == "OpenFile")
        {
            FileLoaded(this, null);
        //     try
        //     {
        //         Window window = await WindowLoader.createWindowAsync(perent,
        //             typeof(EditorView), typeof(EditorViewModel), true);
        //         WindowLoader.setMainWindow(window);
        //         EditorViewModel? editorViewModel = window.DataContext as EditorViewModel;
        //         if (editorViewModel != null && await editorViewModel.OpenDialog())
        //         {
        //             window?.Show();
        //             Window main = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
        //             (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow = window;
        //             main.Close();
        //         }
        //         else
        //         {
        //             MessageBox.Show("Failed to load file or Editor. ");
        //             window?.Close();
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         MessageBox.Show($"Error loading\n{e.Message}", perent);
        //     }
        }
    }

    public ObservableCollection<DataFileItemViewModel> Items { get; } = new ObservableCollection<DataFileItemViewModel>();
}