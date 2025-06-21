using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

public partial class ProjectsPageViewModel : ViewModelBase
{
    private static ProjectsPageViewModel? instance;
    
    private EditorViewModel editorViewModel;

    private readonly IServiceProvider services;

    public ProjectsPageViewModel(IServiceProvider services)
    {
        this.services = services;
        if (instance == null)
        {
            Console.WriteLine("Projects is initialized.");
            instance = this;
            
            Pages = new ObservableCollection<PageTemplate>()
            {
                new PageTemplate(typeof(DataFileViewModel), "Projects"),
                new PageTemplate(typeof(EditorViewModel), "Editor"),
            };
            OnSelectedPageChanged(Pages[0]); // Must be called to properly update UI.
        }
        else
        {
            Pages = instance.Pages;
            services = instance.services;
            CurrentPage = instance.CurrentPage;
            selectedPage = instance.selectedPage;
            editorViewModel = instance.editorViewModel;
            Console.WriteLine("Already is initialized.");
            ScriptMessages.PlayInformationSound();
            OnSelectedPageChanged(Pages[0]); // Must be called to properly update UI.
        }
    }
    
    [ObservableProperty] private ViewModelBase currentPage;
    
    [ObservableProperty] private PageTemplate selectedPage;
    
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

        var instance = services.GetRequiredService(value.PageType);
        if (instance is null) return;
        CurrentPage = (ViewModelBase)instance;
        selectedPage = value;
        if (typeof(EditorViewModel) == value.PageType && editorViewModel is null)
        {
            editorViewModel = (EditorViewModel)instance;
        }
        if (typeof(DataFileViewModel) == value.PageType)
        {
            ((DataFileViewModel)instance).FileLoaded += (s, e) => { OnSelectedPageChanged(Pages[1]); };
        }
    }

    [RelayCommand]
    private void NewProject()
    {
        OnSelectedPageChanged(Pages[1]);
    }
}