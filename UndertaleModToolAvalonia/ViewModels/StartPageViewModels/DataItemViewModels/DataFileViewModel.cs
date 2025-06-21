using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models.StartPageModels;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

public partial class DataFileViewModel : ViewModelBase
{
    private readonly EditorViewModel editorViewModel;

    public EventHandler FileLoaded { get; set; }

    private IFileService fileService;

    public DataFileViewModel(IFileService fileService, EditorViewModel editorViewModel)
    {
        this.fileService = fileService;
        this.editorViewModel = editorViewModel;
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
    public async Task LoadDataAsync(object? parameters)
    {
        if (parameters is not object[] values) return;

        // Unpack the values from the array in the same order you bound them
        var name = values[0] as string;
        var storageProvider = values[1] as Avalonia.Platform.Storage.IStorageProvider;

        if (string.IsNullOrEmpty(name)) return;

        // Now you have both the name and the StorageProvider!
        if (name == "OpenFile")
        {
            if (storageProvider is null)
            {
                // Handle error: couldn't find the StorageProvider
                return;
            }

            // Use the FileService to get a file path
            var files = await fileService.LoadFileAsync(storageProvider);
            var filePath = files?.FirstOrDefault()?.Path.LocalPath;

            if (string.IsNullOrEmpty(filePath))
                return; // User cancelled

            await editorViewModel.LoadFileAsync(filePath);

            FileLoaded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // Logic for loading a recently used file directly
            // await Application.Current.ShowMessage($"Loading data from {name}");
        }
    }

    public ObservableCollection<DataFileItemViewModel> Items { get; } = new ObservableCollection<DataFileItemViewModel>();
}