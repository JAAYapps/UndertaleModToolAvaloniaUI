using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.ComponentModel;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents.UndertaleRoomEditorView;

public partial class UndertaleRoomEditorView : UserControl
{
    private static UndertaleRoom? lastRoom;
    private UndertaleRoomEditorViewModel? currentViewModel;
    private Control? flashingElement;

    public UndertaleRoomEditorView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (currentViewModel is not null)
            currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        currentViewModel = DataContext as UndertaleRoomEditorViewModel;
        if (currentViewModel is null)
            return;

        if (!ReferenceEquals(lastRoom, currentViewModel.Room))
        {
            UndertaleCachedImageLoader.Reset();
            CachedTileDataLoader.Reset();
            lastRoom = currentViewModel.Room;
        }

        currentViewModel.SetupRoom();
        currentViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UndertaleRoomEditorViewModel.SelectedObject))
            FlashObject(currentViewModel?.SelectedObject);
    }

    private void FlashObject(object? obj)
    {
        flashingElement?.Classes.Remove("flash");
        flashingElement = null;

        if (obj is null or UndertaleRoom or View or Layer { IsVisible: false })
            return;

        flashingElement = RoomGraphics.GetVisualDescendants().OfType<Control>()
                                      .FirstOrDefault(x => x is not ContentPresenter && ReferenceEquals(x.DataContext, obj));
        flashingElement?.Classes.Add("flash");
    }

    private void RoomObjectsTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        object? selected = RoomObjectsTree.SelectedItem;
        if (ReferenceEquals(selected, RoomRootItem))
            vm.SelectedObject = vm.Room;
        else if (selected is UndertaleObject obj)
            vm.SelectedObject = obj;
    }

    private async void RoomObjectsTree_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        UndertaleObject? selectedObj = vm.SelectedObject as UndertaleObject;

        if (e.Key == Key.Delete)
            vm.DeleteItemCommand.Execute(selectedObj);
        else if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
            vm.CopyCommand.Execute(selectedObj);
        else if (e.Key == Key.X && selectedObj is GameObject { ObjectDefinition.Sprite: not null } objx)
        {
            objx.ScaleX *= -1;
            objx.X -= (((int)objx.ObjectDefinition.Sprite.Width - objx.ObjectDefinition.Sprite.OriginX) * (int)objx.ScaleX);
        }
        else if (e.Key == Key.Y && selectedObj is GameObject { ObjectDefinition.Sprite: not null } objy)
        {
            objy.ScaleY *= -1;
            objy.Y -= (((int)objy.ObjectDefinition.Sprite.Height - objy.ObjectDefinition.Sprite.OriginY) * (int)objy.ScaleY);
        }
        else if (e.Key == Key.OemMinus)
            await MoveItem(selectedObj, -1);
        else if (e.Key == Key.OemPlus)
            await MoveItem(selectedObj, 1);
    }

    private void RoomObjectsTree_DoubleTapped(object? sender, TappedEventArgs e)
    {
        OpenDefinition(RoomObjectsTree.SelectedItem, false);
    }

    private void RoomObjectsTree_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(RoomObjectsTree).Properties.IsMiddleButtonPressed)
            return;

        TreeViewItem? treeViewItem = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(includeSelf: true);
        if (treeViewItem is null)
            return;

        treeViewItem.IsSelected = true;
        treeViewItem.Focus();
        OpenDefinition(treeViewItem.DataContext, true);
    }

    private static void OpenDefinition(object? sel, bool inNewTab)
    {
        object? definition = sel switch
        {
            GameObject gameObj => gameObj.ObjectDefinition,
            UndertaleRoom.Background bg => bg.BackgroundDefinition,
            Tile tile => tile.ObjectDefinition,
            SpriteInstance sprInst => sprInst.Sprite,
            ParticleSystemInstance partSys => partSys.ParticleSystem,
            _ => null
        };
        if (definition is not null)
            WeakReferenceMessenger.Default.Send(new OpenAssetMessage(definition, inNewTab));
    }

    private async void TreeViewMoveUpButton_Click(object? sender, RoutedEventArgs e)
    {
        if (RoomObjectsTree.SelectedItem is UndertaleObject selectedObj)
            await MoveItem(selectedObj, -1, false);
    }

    private async void TreeViewMoveDownButton_Click(object? sender, RoutedEventArgs e)
    {
        if (RoomObjectsTree.SelectedItem is UndertaleObject selectedObj)
            await MoveItem(selectedObj, 1, false);
    }

    private async System.Threading.Tasks.Task MoveItem(UndertaleObject? obj, int dist, bool focus = true)
    {
        if (obj is null || DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        bool isInAllGameObjectsList = false;
        if (obj is GameObject && vm.IsGMS2Room)
        {
            if (GameObjItems.ContainerFromItem(obj) is TreeViewItem { IsSelected: true })
                isInAllGameObjectsList = true;
        }

        if (await vm.MoveItem(obj, dist, isInAllGameObjectsList))
            SelectObject(obj, focus, isInAllGameObjectsList);
    }

    private void SelectObject(UndertaleObject obj, bool focus = true, bool isInAllGameObjectsList = false)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        UndertaleRoom room = vm.Room;
        TreeViewItem? resListView = null;
        switch (obj)
        {
            case UndertaleRoom.Background:
                resListView = BGItems;
                break;

            case View:
                resListView = ViewItems;
                break;

            case GameObject gameObj:
                if (vm.IsGMS2Room && !isInAllGameObjectsList)
                {
                    Layer? resLayer = room.Layers.FirstOrDefault(l => l.LayerType is LayerType.Instances
                                                                      && (l.InstancesData.Instances?.Contains(gameObj) ?? false));
                    resListView = resLayer is null ? null : LayerItems.ContainerFromItem(resLayer) as TreeViewItem;
                }
                else
                    resListView = GameObjItems;
                break;

            case Tile tile:
                if (vm.IsGMS2Room)
                {
                    Layer? resLayer = room.Layers.FirstOrDefault(l => l.LayerType is LayerType.Assets
                                                                      && (l.AssetsData.LegacyTiles?.Contains(tile) ?? false));
                    resListView = resLayer is null ? null : LayerItems.ContainerFromItem(resLayer) as TreeViewItem;
                }
                else
                    resListView = TileItems;
                break;

            case Layer:
                resListView = LayerItems;
                break;

            case SpriteInstance spr:
                {
                    Layer? resLayer = room.Layers.FirstOrDefault(l => l.LayerType is LayerType.Assets
                                                                      && (l.AssetsData.Sprites?.Contains(spr) ?? false));
                    resListView = resLayer is null ? null : LayerItems.ContainerFromItem(resLayer) as TreeViewItem;
                }
                break;

            case ParticleSystemInstance partSys:
                {
                    Layer? resLayer = room.Layers.FirstOrDefault(l => l.LayerType is LayerType.Assets
                                                                      && (l.AssetsData.ParticleSystems?.Contains(partSys) ?? false));
                    resListView = resLayer is null ? null : LayerItems.ContainerFromItem(resLayer) as TreeViewItem;
                }
                break;
        }

        if (resListView is null)
            return;

        resListView.IsExpanded = true;
        resListView.UpdateLayout();

        if (resListView.ContainerFromItem(obj) is TreeViewItem resItem)
        {
            resItem.IsSelected = true;
            resItem.BringIntoView();
            if (focus)
                resItem.Focus();
        }
    }

    private void MenuItem_Copy_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        vm.CopyCommand.Execute((sender as MenuItem)?.DataContext as UndertaleObject);
    }

    private void MenuItem_Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        vm.DeleteItemCommand.Execute((sender as MenuItem)?.DataContext as UndertaleObject);
    }
}

public class LayerDataTemplateSelector : ITreeDataTemplate
{
    public ITreeDataTemplate? PathDataTemplate { get; set; }
    public ITreeDataTemplate? InstancesDataTemplate { get; set; }
    public ITreeDataTemplate? TilesDataTemplate { get; set; }
    public ITreeDataTemplate? AssetsDataTemplate { get; set; }
    public ITreeDataTemplate? BackgroundDataTemplate { get; set; }
    public ITreeDataTemplate? EffectDataTemplate { get; set; }

    private ITreeDataTemplate? SelectTemplate(object? item)
    {
        if (item is Layer layer)
        {
            switch (layer.LayerType)
            {
                case LayerType.Path:
                case LayerType.Path2:
                    return PathDataTemplate;
                case LayerType.Instances:
                    return InstancesDataTemplate;
                case LayerType.Tiles:
                    return TilesDataTemplate;
                case LayerType.Assets:
                    return AssetsDataTemplate;
                case LayerType.Background:
                    return BackgroundDataTemplate;
                case LayerType.Effect:
                    return EffectDataTemplate;
            }
        }

        return null;
    }

    public Control? Build(object? param)
    {
        return SelectTemplate(param)?.Build(param);
    }

    public bool Match(object? data)
    {
        return data is Layer;
    }

    public IDisposable? BindChildren(AvaloniaObject target, AvaloniaProperty targetProperty, object item)
    {
        return SelectTemplate(item)?.BindChildren(target, targetProperty, item);
    }
}