using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Project;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.ProjectService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public record UnexportedAsset(string Name, string AssetType, IProjectAsset ProjectAsset);

    public partial class ProjectAssetsViewModel : ViewModelBase, IInitializable<ProjectAssetsParameters>
    {
        private readonly IProjectService? projectService;

        private ProjectContext? project;

        private bool preventUpdateList;

        public string Title { get; } = "Unexported project assets";

        public ObservableCollection<UnexportedAsset> Assets { get; } = new();

        [ObservableProperty]
        private UnexportedAsset? selectedAsset;

        public event EventHandler? CloseRequested;

        public ProjectAssetsViewModel() { }

        public ProjectAssetsViewModel(IProjectService projectService)
        {
            this.projectService = projectService;
        }

        public Task<bool> InitializeAsync(ProjectAssetsParameters parameters)
        {
            project = parameters.Project;
            project.UnexportedAssetsChanged += Project_UnexportedAssetsChanged;
            if (projectService is not null)
                projectService.ProjectChanged += ProjectService_ProjectChanged;
            UpdateList();
            return Task.FromResult(true);
        }

        public void Detach()
        {
            if (project is not null)
                project.UnexportedAssetsChanged -= Project_UnexportedAssetsChanged;
            if (projectService is not null)
                projectService.ProjectChanged -= ProjectService_ProjectChanged;
        }

        private void Project_UnexportedAssetsChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(UpdateList);
        }

        private void ProjectService_ProjectChanged(object? sender, EventArgs e)
        {
            if (!ReferenceEquals(projectService?.Project, project))
                CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateList()
        {
            if (preventUpdateList || project is null)
                return;

            List<UnexportedAsset> assets = project
                .EnumerateUnexportedAssets()
                .Select(asset => new UnexportedAsset(asset.ProjectName, asset.ProjectAssetType.ToInterfaceName(), asset))
                .ToList();
            assets.Sort((a, b) =>
            {
                if (a.AssetType.CompareTo(b.AssetType) is int i && i != 0)
                    return i;
                return a.Name.CompareTo(b.Name);
            });

            Assets.Clear();
            foreach (UnexportedAsset asset in assets)
                Assets.Add(asset);
        }

        [RelayCommand]
        private void Open(UnexportedAsset? asset)
        {
            OpenAsset(asset, false);
        }

        [RelayCommand]
        private void OpenInNewTab(UnexportedAsset? asset)
        {
            OpenAsset(asset, true);
        }

        private static void OpenAsset(UnexportedAsset? asset, bool inNewTab)
        {
            if (asset?.ProjectAsset is not UndertaleObject obj)
                return;

            WeakReferenceMessenger.Default.Send(new OpenAssetMessage(obj, inNewTab));
        }

        [RelayCommand]
        private void UnmarkForExport(IList? items)
        {
            if (project is null || items is null)
                return;

            preventUpdateList = true;
            foreach (UnexportedAsset asset in items.OfType<UnexportedAsset>().ToList())
                project.UnmarkAssetForExport(asset.ProjectAsset);
            preventUpdateList = false;
            UpdateList();
        }

        public static bool CanMarkForExport(object? obj)
        {
            return obj is IProjectAsset { ProjectExportable: true };
        }

        public bool MarkForExport(object? obj)
        {
            return project is not null && obj is IProjectAsset projectAsset && project.MarkAssetForExport(projectAsset);
        }
    }
}
