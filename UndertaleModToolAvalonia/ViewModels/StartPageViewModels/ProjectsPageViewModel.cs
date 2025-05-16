using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Core;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Utility;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

public partial class ProjectsPageViewModel : ViewModelBase
{
    private static ProjectsPageViewModel _instance;
    
    private EditorViewModel _editorViewModel;
    
    public ProjectsPageViewModel()
    {
        if (_instance == null)
        {
            Console.WriteLine("Projects is initialized.");
            _instance = this;
            
            Pages = new ObservableCollection<PageTemplate>()
            {
                new PageTemplate(typeof(DataFileViewModel), "Projects"),
                new PageTemplate(typeof(EditorViewModel), "Editor"),
            };
            OnSelectedPageChanged(Pages[0]);
            
        }
        else
        {
            Pages = _instance.Pages;
            CurrentPage = _instance.CurrentPage;
            selectedPage = _instance.selectedPage;
            _editorViewModel = _instance._editorViewModel;
            Console.WriteLine("Already is initialized.");
            ScriptMessages.PlayInformationSound();
        }
    }
    
    [ObservableProperty] private ViewModelBase currentPage;
    
    [ObservableProperty] private PageTemplate selectedPage;
    
    public ObservableCollection<PageTemplate> Pages { get; }
    
    partial void OnSelectedPageChanged(PageTemplate? value)
    {
        if (value is null) return;
        if (typeof(EditorViewModel) == value.PageType && _editorViewModel is not null)
        {
            CurrentPage = (ViewModelBase)_editorViewModel;
            selectedPage = value;
            return;
        }

        var instance = Activator.CreateInstance(value.PageType);
        if (instance is null) return;
        CurrentPage = (ViewModelBase)instance;
        selectedPage = value;
        if (typeof(EditorViewModel) == value.PageType && _editorViewModel is null)
        {
            _editorViewModel = (EditorViewModel)instance;
        }
        if (typeof(DataFileViewModel) == value.PageType)
        {
            (instance as DataFileViewModel).FileLoaded += (s, e) => { Console.WriteLine("File Loaded"); OnSelectedPageChanged(Pages[1]); Console.WriteLine("Page Loaded"); };
        }
    }
}