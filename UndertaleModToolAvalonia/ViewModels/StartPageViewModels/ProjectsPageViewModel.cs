using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

public partial class ProjectsPageViewModel : ViewModelBase
{
    private static ProjectsPageViewModel? instance;
    
    private EditorViewModel? editorViewModel;

    private readonly IServiceProvider? services;

    private IFileService? fileService;

    public ProjectsPageViewModel()
    {
        Pages = new ObservableCollection<PageTemplate>()
        {
            new PageTemplate(typeof(DataFileViewModel), "Projects"),
            new PageTemplate(typeof(EditorViewModel), "Editor"),
        };
    }

    public ProjectsPageViewModel(IServiceProvider services, IFileService fileService)
    {
        this.services = services;
        this.fileService = fileService;
        if (instance == null)
        {
            Console.WriteLine("Projects is initialized.");
            
            Pages = new ObservableCollection<PageTemplate>()
            {
                new PageTemplate(typeof(DataFileViewModel), "Projects"),
                new PageTemplate(typeof(EditorViewModel), "Editor"),
            };
            instance = this;
            OnSelectedPageChanged(Pages[0]); // Must be called to properly update UI.
        }
        else
        {
            Pages = instance.Pages;
            CurrentPage = instance.CurrentPage;
            selectedPage = instance.selectedPage;
            editorViewModel = instance.editorViewModel;
            Console.WriteLine("Already is initialized.");
            ScriptMessages.PlayInformationSound();
            instance = this;
            OnSelectedPageChanged(SelectedPage); // Must be called to properly update UI.
        }
    }
    
    [ObservableProperty] private ViewModelBase? currentPage;
    
    [ObservableProperty] private PageTemplate? selectedPage;
    
    public ObservableCollection<PageTemplate> Pages { get; }
    
    partial void OnSelectedPageChanged(PageTemplate value)
    {
        if (value is null) return;
        if (typeof(EditorViewModel) == value.PageType && editorViewModel is not null)
        {
            CurrentPage = (ViewModelBase)editorViewModel;
            selectedPage = value;
            return;
        }

        var newInstance = services?.GetRequiredService(value.PageType);
        if (newInstance is null) return;
        CurrentPage = (ViewModelBase)newInstance;
        selectedPage = value;
        if (typeof(EditorViewModel) == value.PageType && editorViewModel is null)
        {
            editorViewModel = (EditorViewModel)newInstance;
        }
        if (typeof(DataFileViewModel) == value.PageType)
        {
            ((DataFileViewModel)newInstance).FileLoaded += (s, e) => { OnSelectedPageChanged(Pages[1]); };
        }
    }

    [RelayCommand]
    private void NewProject()
    {
        OnSelectedPageChanged(Pages[1]);
    }

    [RelayCommand]
    private async Task OpenFile(IStorageProvider storageProvider)
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

        OnSelectedPageChanged(Pages[1]);
        await editorViewModel.LoadFileAsync(filePath);
    }
}