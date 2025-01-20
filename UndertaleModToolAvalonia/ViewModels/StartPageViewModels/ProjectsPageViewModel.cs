using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Core;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

public partial class ProjectsPageViewModel : ViewModelBase
{
    public ProjectsPageViewModel()
    {
        fileViewModel = new DataFileViewModel();
    }
    
    [ObservableProperty] private DataFileViewModel fileViewModel;
}