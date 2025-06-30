using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.Views.EditorViews.FindReferencesTypesDialog;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog
{
    public partial class SelectableType : ObservableObject
    {
        [ObservableProperty]
        private bool isSelected;
        public Type Type { get; }
        public string Name { get; }

        public SelectableType(Type type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public partial class FindReferencesTypesDialogViewModel : ViewModelBase, IInitializable<UndertaleReferenceParameters>
    {
        private readonly IReferenceFinderService? referenceFinder;
        private readonly IDialogService? dialogService;

        public UndertaleResource? SourceObj { get; private set; }

        public UndertaleData? Data { get; private set; }

        // Holding the state for the checkboxes here.
        // Avalonia will use this in axaml in ItemsSource property with a CheckBox as a DataTemplete.
        [ObservableProperty]
        private ObservableCollection<SelectableType> typesList = new();

        public FindReferencesTypesDialogViewModel() { }

        public FindReferencesTypesDialogViewModel(IReferenceFinderService referenceFinder, IDialogService dialogService)
        {
            this.referenceFinder = referenceFinder;
            this.dialogService = dialogService;
        }

        public async Task<bool> InitializeAsync(UndertaleReferenceParameters parameters)
        {
            if (parameters == null)
            {
                await App.Current!.ShowError("Parameters were not valid.");
                return false;
            }
            
            if (Data?.GeneralInfo is null)
            {
                await App.Current!.ShowError("Cannot determine GameMaker version - \"General Info\" is null.");
                return false;
            }

            if (parameters.obj == null)
                return await Init(parameters.data);
            else
                return await InitWithSourceObject(parameters.obj, parameters.data);
        }

        private async Task<bool> InitWithSourceObject(UndertaleResource? obj, UndertaleData? data)
        {
            (Type, string)[] sourceTypes = UndertaleResourceReferenceMap.GetTypeMapForVersion(obj!.GetType(), data!);
            if (sourceTypes is null)
            {
                await App.Current!.ShowError($"Cannot get the source types for object of type \"{obj.GetType()}\".");
                return false;
            }

            foreach (var typePair in sourceTypes)
            {
                // Making sure that the UI properly updates the list.
                TypesList.Add(new(typePair.Item1,typePair.Item2)
                {
                    IsSelected = true
                });
            }

            SourceObj = obj;
            this.Data = data;
            return true;
        }

        private async Task<bool> Init(UndertaleData? data)
        {
            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            var sourceTypes = UndertaleResourceReferenceMap.GetReferenceableTypes(ver);

            foreach (var typePair in sourceTypes)
            {
                if (data.Code is null && UndertaleResourceReferenceMap.CodeTypes.Contains(typePair.Key))
                    continue;

                TypesList.Add(new (typePair.Key,typePair.Value)
                {
                    IsSelected = true
                });
            }

            this.Data = data;
            return true;
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var type in TypesList)
            {
                type.IsSelected = true;
            }
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var type in TypesList)
            {
                type.IsSelected = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (SourceObj is not null)
            {
                HashSetTypesOverride typesList = new();
                foreach (var item in TypesList)
                {
                    if (item is SelectableType checkBox && checkBox.IsSelected == true)
                    {
                        typesList.Add(checkBox.Type);
                    }
                }

                if (typesList.Count == 0)
                {
                    await App.Current!.ShowError("At least one type should be selected.");
                    return;
                }

                try
                {
                    var results = await referenceFinder!.GetReferencesOfObject(SourceObj!, Data!, typesList);
                    await dialogService!.ShowAsync<FindReferencesResultsViewModel, FindReferencesResultsParameters>(new FindReferencesResultsParameters(SourceObj!, Data!, results));
                }
                catch (Exception ex)
                {
                    await App.Current!.ShowError("An error occurred in the object references related window.\n" +
                                         $"Please report this on GitHub.\n\n{ex}");
                }

            }
            else
            {
                Dictionary<Type, string> typesDict = new();
                foreach (var item in TypesList)
                {
                    if (item is SelectableType checkBox && checkBox.IsSelected == true)
                    {
                        typesDict[checkBox.Type] = checkBox.Name;
                    }
                }

                if (typesDict.Count == 0)
                {
                    await App.Current!.ShowError("At least one type should be selected.");
                    return;
                }

                if (typesDict.Count > 1 && typesDict.ContainsKey(typeof(UndertaleString))
                    && Data?.Strings.Count > 5000)
                {
                    var res = await App.Current!.ShowQuestion("You have selected the \"Strings\" when there are a lot of strings.\n" +
                                                "That could make the search process noticeably longer.\n" +
                                                "Do you want to proceed?");
                    if (res != MsBox.Avalonia.Enums.ButtonResult.Yes)
                        return;
                }

                try
                {
                    var results = await referenceFinder!.GetUnreferencedObjects(Data!, typesDict);
                    await dialogService!.ShowAsync<FindReferencesResultsViewModel, FindReferencesResultsParameters>(new FindReferencesResultsParameters(null, Data!, results));
                }
                catch (Exception ex)
                {
                    await App.Current!.ShowError("An error occurred in the object references related window.\n" +
                                         $"Please report this on GitHub.\n\n{ex}");
                }
            }
            WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
        }
    }
}
