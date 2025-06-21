using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using UndertaleModLib;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

namespace UndertaleModToolAvalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider services;

        [ObservableProperty] private string titleMain = string.Empty;
        
        [ObservableProperty] public UndertaleData? data = AppConstants.Data;

        [ObservableProperty] string? filePath = AppConstants.FilePath;
        
        public MainWindowViewModel(IServiceProvider services)
        {
            this.services = services;
            TitleMain = "UndertaleModTool by krzys_h, recreated by Joshua Vanderzee v:" + AppConstants.Version;
            
            Pages = new ObservableCollection<PageTemplate>()
            {
                new PageTemplate(typeof(ProjectsPageViewModel), "Projects"),
                new PageTemplate(typeof(SettingsPageViewModel), "Settings"),
            };
            OnSelectedPageChanged(Pages[0]);
        }

        [ObservableProperty] private bool isPaneOpen = false;
        
        [ObservableProperty] private ViewModelBase currentPage;

        [ObservableProperty] private PageTemplate selectedPage;

        partial void OnSelectedPageChanged(PageTemplate value)
        {
            if (value is null) return;
            var instance = services.GetRequiredService(value.PageType);
            if (instance is null) return;
            CurrentPage = (ViewModelBase)instance;
            selectedPage = value;
        }
        
        public ObservableCollection<PageTemplate> Pages { get; }

        [RelayCommand]
        private void OnPaneOpen()
        {
            IsPaneOpen = !IsPaneOpen;
        }
    }
}
