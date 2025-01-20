using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.Core;
using UndertaleModLib;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

namespace UndertaleModToolAvalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string titleMain = string.Empty;
        
        [ObservableProperty] public UndertaleData? data = AppConstants.Data;

        [ObservableProperty] string? filePath = AppConstants.FilePath;

        // Version info
        public static string Edition = "(Git: " + GitVersion.GetGitVersion().Substring(0, 7) + ")";
        
        // On debug, build with git versions and provided release version. Otherwise, use the provided release version only.
#if DEBUG || SHOW_COMMIT_HASH
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString() + (Edition != "" ? " - " + Edition : "");
#else
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
        
        public MainWindowViewModel()
        {
            TitleMain = "UndertaleModTool by krzys_h, recreated by Joshua Vanderzee v:" + Version;
            
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

        partial void OnSelectedPageChanged(PageTemplate? value)
        {
            if (value is null) return;
            var instance = Activator.CreateInstance(value.PageType);
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
