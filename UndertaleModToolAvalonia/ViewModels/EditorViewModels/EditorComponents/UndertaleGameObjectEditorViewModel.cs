using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Models.UndertaleReferenceTypes;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.ReferenceFinderService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class GameObjectEventTypeViewModel(EventType eventType, UndertalePointerList<UndertaleGameObject.Event> events) : ObservableObject
    {
        public EventType EventType { get; } = eventType;

        public string Name { get; } = eventType.ToString();

        public UndertalePointerList<UndertaleGameObject.Event> Events { get; } = events;

        public bool IsKeyEvent => EventType is EventType.Keyboard or EventType.KeyPress or EventType.KeyRelease;

        public bool IsStepEvent => EventType == EventType.Step;

        public bool IsMouseEvent => EventType == EventType.Mouse;

        public bool IsOtherEvent => EventType == EventType.Other;

        public bool IsDrawEvent => EventType == EventType.Draw;

        public bool IsCollisionEvent => EventType == EventType.Collision;

        public bool HasNoSubtype => EventType is EventType.Create or EventType.Destroy;

        public bool IsSubtypeTextVisible => IsKeyEvent || !(IsStepEvent || IsMouseEvent || IsOtherEvent || IsDrawEvent || IsCollisionEvent || HasNoSubtype);
    }

    public partial class UndertaleGameObjectEditorViewModel(string title, UndertaleGameObject undertaleGameObject, EditorViewModel editor, IFileService fileService, ITextureCacheService textureCacheService, IReferenceFinderService referenceFinderService, IDialogService dialogService) : EditorContentViewModel(title)
    {
        private bool showingNoChildrenFoundMessage = false;

        public EditorViewModel Editor { get; } = editor;
        public ITextureCacheService TextureCacheService { get; } = textureCacheService;
        public IFileService FileService { get; } = fileService;

        [ObservableProperty]
        private UndertaleGameObject undertaleGameObject = undertaleGameObject;

        [ObservableProperty]
        private string showChildrenText = "Show children game objects";

        public bool IsDepthVisible => !UndertaleHelper.IsGMS2;

        public CollisionShapeFlags[] CollisionShapes { get; } = 
            Enum.GetValues<CollisionShapeFlags>();

        public EventSubtypeKey[] EventSubtypeKeys { get; } = 
            Enum.GetValues<EventSubtypeKey>();

        public EventSubtypeStep[] EventSubtypeSteps { get; } = 
            Enum.GetValues<EventSubtypeStep>();

        public EventSubtypeMouse[] EventSubtypeMouses { get; } = 
            Enum.GetValues<EventSubtypeMouse>();

        public EventSubtypeOther[] EventSubtypeOthers { get; } = 
            Enum.GetValues<EventSubtypeOther>();

        public EventSubtypeDraw[] EventSubtypeDraws { get; } = 
            Enum.GetValues<EventSubtypeDraw>();

        public List<GameObjectEventTypeViewModel> EventTypes { get; } = 
            undertaleGameObject.Events.Select((events, index) => new GameObjectEventTypeViewModel((EventType)index, events)).ToList();

        [RelayCommand]
        private void AddVertex()
        {
            UndertaleGameObject.PhysicsVertices.Add(new UndertaleGameObject.UndertalePhysicsVertex());
        }

        [RelayCommand]
        private void AddEvent(GameObjectEventTypeViewModel eventType)
        {
            if (eventType is null) return;

            UndertaleGameObject.Event obj = new();
            obj.Actions.Add(new UndertaleGameObject.EventAction());
            eventType.Events.Add(obj);
        }

        [RelayCommand]
        private void AddAction(UndertaleGameObject.Event ev)
        {
            if (ev is null) return;

            ev.Actions.Add(new UndertaleGameObject.EventAction());
        }

        [RelayCommand]
        private void RemoveAction(UndertaleGameObject.EventAction action)
        {
            if (action is null) return;

            foreach (var evList in UndertaleGameObject.Events)
            {
                var ev = evList.FirstOrDefault(x => x.Actions.Contains(action));
                if (ev is null) continue;

                ev.Actions.Remove(action);
                if (ev.Actions.Count <= 0)
                {
                    evList.Remove(ev);
                }
                return;
            }
        }

        [RelayCommand]
        private async Task ShowChildren()
        {
            try
            {
                var typeList = new HashSetTypesOverride() { typeof(UndertaleGameObject) };
                var results = referenceFinderService.GetReferencesOfObject(UndertaleGameObject, AppConstants.Data!, typeList);
                if (results is null)
                {
                    if (showingNoChildrenFoundMessage) return;

                    string originalText = ShowChildrenText;

                    showingNoChildrenFoundMessage = true;
                    ShowChildrenText = "(no children were found)";
                    await Task.Delay(2000);

                    ShowChildrenText = originalText;
                    showingNoChildrenFoundMessage = false;
                    return;
                }

                await dialogService.ShowAsync<FindReferencesResultsViewModel, FindReferencesResultsParameters>(new FindReferencesResultsParameters(UndertaleGameObject, AppConstants.Data!, results));
            }
            catch (Exception ex)
            {
                await App.Current!.ShowError("An error occurred in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
        }
    }
}
