using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents.UndertaleRoomEditorView;

public partial class UndertaleRoomEditorView : UserControl
{
    private static UndertaleRoom? lastRoom;
    private UndertaleRoomEditorViewModel? currentViewModel;
    private Control? flashingElement;

    private UndertaleObject? selectedObject;
    private bool selectedInAllGameObjectsList;
    private UndertaleObject? movingObj;
    private double hotpointX, hotpointY;
    private bool leftDrag, rightDrag, topDrag, bottomDrag;
    private Point dragObjectOrigin;
    private bool placingTiles = false;
    private readonly List<Point> placedTiles = new();
    private double scaleOriginX, scaleOriginY;
    private Point lastMousePos;
    private bool selectingObject;
    private bool tabContentStateRestored;

    public UndertaleRoomEditorView()
    {
        InitializeComponent();
        RoomObjectsTree.AddHandler(GotFocusEvent, RoomObjectsTree_GotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(KeyDownEvent, UndertaleRoomEditorView_KeyDown, RoutingStrategies.Bubble);
        ZoomBorder.ZoomChanged += ZoomBorder_ZoomChanged;
        ZoomBorder.AddHandler(DragDrop.DragOverEvent, RoomGraphics_DragOver);
        ZoomBorder.AddHandler(DragDrop.DropEvent, RoomGraphics_Drop);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (currentViewModel is not null)
        {
            currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            SaveTabContentState(currentViewModel);
        }
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
        GameObjItems.ContextFlyout = currentViewModel.IsGMS2Room ? null : Resources["GameObjectsFlyout"] as MenuFlyout;
        currentViewModel.PropertyChanged += ViewModel_PropertyChanged;

        tabContentStateRestored = false;
        UndertaleRoomEditorViewModel vm = currentViewModel;
        Dispatcher.UIThread.Post(() => RestoreTabContentState(vm), DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (currentViewModel is not null)
            SaveTabContentState(currentViewModel);

        base.OnDetachedFromVisualTree(e);
    }

    private void ZoomBorder_ZoomChanged(object sender, Avalonia.Controls.PanAndZoom.ZoomChangedEventArgs e)
    {
        if (currentViewModel is null || !tabContentStateRestored)
            return;

        currentViewModel.RoomScale = e.ZoomX;
        currentViewModel.ScrollX = e.OffsetX;
        currentViewModel.ScrollY = e.OffsetY;
    }

    private void SaveTabContentState(UndertaleRoomEditorViewModel vm)
    {
        vm.ObjectTreeItemsStates = new bool[]
        {
            BGItems.IsExpanded,
            ViewItems.IsExpanded,
            GameObjItems.IsExpanded,
            TileItems.IsExpanded,
            LayerItems.IsExpanded
        };
        if (RoomObjectsTree.FindDescendantOfType<ScrollViewer>() is ScrollViewer treeViewer)
            vm.ObjectsTreeScrollPosition = treeViewer.Offset;
    }

    private void RestoreTabContentState(UndertaleRoomEditorViewModel vm)
    {
        if (!ReferenceEquals(vm, currentViewModel))
            return;

        ZoomBorder.SetMatrix(new Matrix(vm.RoomScale, 0, 0, vm.RoomScale, vm.ScrollX, vm.ScrollY));
        tabContentStateRestored = true;

        if (vm.ObjectTreeItemsStates is not null)
        {
            BGItems.IsExpanded = vm.ObjectTreeItemsStates[0];
            ViewItems.IsExpanded = vm.ObjectTreeItemsStates[1];
            GameObjItems.IsExpanded = vm.ObjectTreeItemsStates[2];
            TileItems.IsExpanded = vm.ObjectTreeItemsStates[3];
            LayerItems.IsExpanded = vm.ObjectTreeItemsStates[4];
            RoomRootItem.UpdateLayout();
        }

        if (vm.SelectedObject is UndertaleRoom)
            RoomObjectsTree.SelectedItem = RoomRootItem;
        else if (vm.SelectedObject is UndertaleObject obj)
            SelectObject(obj, false);

        FlashObject(vm.SelectedObject);

        Vector treeScrollPos = vm.ObjectsTreeScrollPosition;
        if (vm.ObjectTreeItemsStates is not null && RoomObjectsTree.FindDescendantOfType<ScrollViewer>() is ScrollViewer treeViewer)
            Dispatcher.UIThread.Post(() => treeViewer.Offset = treeScrollPos, DispatcherPriority.Background);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UndertaleRoomEditorViewModel.SelectedObject))
            return;

        object? selected = currentViewModel?.SelectedObject;
        if (!selectingObject && selected is UndertaleObject obj and not UndertaleRoom
            && !ReferenceEquals(RoomObjectsTree.SelectedItem, obj))
            SelectObject(obj, false);

        FlashObject(selected);
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
        {
            vm.SelectedObject = obj;
            selectedObject = obj;
        }
    }

    private void RoomObjectsTree_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        TreeViewItem? treeViewItem = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(includeSelf: true);
        if (treeViewItem?.DataContext is GameObject)
            selectedInAllGameObjectsList = ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(treeViewItem), GameObjItems);
    }

    private async void RoomObjectsTree_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        UndertaleObject? selectedObj = vm.SelectedObject as UndertaleObject;

        if (e.Key == Key.Delete)
            vm.DeleteItemCommand.Execute(selectedObj);
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            return;
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

    private void UndertaleRoomEditorView_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        if ((e.Source as Visual)?.FindAncestorOfType<TextBox>(includeSelf: true) is TextBox { IsReadOnly: false })
            return;

        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            vm.CopyCommand.Execute(vm.SelectedObject as UndertaleObject);
        else if (e.KeyModifiers == KeyModifiers.Control && (e.Key == Key.V || e.Key == Key.P))
            Paste();
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.V)
            Paste(true);
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Z)
            vm.UndoCommand.Execute(null);
        else
            return;

        e.Handled = true;
    }

    private void Paste(bool shiftPressed = false)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        Point mousePos = ZoomBorder.IsPointerOver ? lastMousePos : new();
        UndertaleObject? newObj = vm.Paste(mousePos, shiftPressed);

        if (newObj is not null)
            SelectObject(newObj);
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

        bool isInAllGameObjectsList = obj is GameObject && vm.IsGMS2Room && selectedInAllGameObjectsList;

        if (await vm.MoveItem(obj, dist, isInAllGameObjectsList))
            SelectObject(obj, focus, isInAllGameObjectsList);
    }

    private void SelectObject(UndertaleObject obj, bool focus = true, bool isInAllGameObjectsList = false)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        selectedObject = obj;
        obj = obj switch
        {
            Layer.LayerBackgroundData bgData => bgData.ParentLayer,
            Layer.LayerTilesData tileData => tileData.ParentLayer,
            _ => obj
        };

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

        selectingObject = true;
        try
        {
            vm.SelectedObject = obj;

            if (resListView is null)
                return;

            resListView.IsExpanded = true;
            resListView.UpdateLayout();
            RoomObjectsTree.SelectedItem = obj;
        }
        finally
        {
            selectingObject = false;
        }

        if (!ShowTreeItem(resListView, obj, focus))
            Dispatcher.UIThread.Post(() => ShowTreeItem(resListView, obj, focus), DispatcherPriority.Background);
    }

    private static bool ShowTreeItem(TreeViewItem resListView, UndertaleObject obj, bool focus)
    {
        if (resListView.ContainerFromItem(obj) is not TreeViewItem resItem)
            return false;

        resItem.BringIntoView();
        if (focus)
            resItem.Focus();

        return true;
    }

    private Point GetGridMouseCoordinates(Point mousePos, UndertaleRoom room, KeyModifiers modifiers)
    {
        int gridWidth = Math.Max(Convert.ToInt32(room.GridWidth), 1);
        int gridHeight = Math.Max(Convert.ToInt32(room.GridHeight), 1);

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            if (gridWidth > 1)
                gridWidth /= 2;
            if (gridHeight > 1)
                gridHeight /= 2;
        }
        else if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            gridWidth *= 2;
            gridHeight *= 2;
        }

        return new Point(Math.Floor(mousePos.X / gridWidth) * gridWidth, Math.Floor(mousePos.Y / gridHeight) * gridHeight);
    }

    private static bool IsTileLayerEmptyAt(Layer.LayerTilesData tilesData, Point pos)
    {
        if (tilesData.Background is null || tilesData.Background.GMS2TileWidth == 0 || tilesData.Background.GMS2TileHeight == 0)
            return true;
        if (pos.X < 0 || pos.Y < 0)
            return true;

        int x1 = (int)pos.X / (int)tilesData.Background.GMS2TileWidth;
        int y1 = (int)pos.Y / (int)tilesData.Background.GMS2TileHeight;

        return x1 > tilesData.TilesX - 1 ||
               y1 > tilesData.TilesY - 1 ||
               tilesData.TileData[y1][x1] == 0;
    }

    private static void CollectRoomObjectElements(Visual visual, List<Control> elements)
    {
        if (!visual.IsVisible)
            return;

        if (visual is Control element && visual is not ContentPresenter && visual is not ItemsControl
            && element.DataContext is GameObject or Tile or SpriteInstance or ParticleSystemInstance or UndertaleRoom.Background
                                      or Layer.LayerBackgroundData or Layer.LayerTilesData)
            elements.Add(element);

        foreach (Visual child in visual.GetVisualChildren().OrderBy(x => x.ZIndex))
            CollectRoomObjectElements(child, elements);
    }

    private Control? GetRoomObjectElementAt(Point mousePos)
    {
        List<Control> elements = new();
        CollectRoomObjectElements(RoomGraphics, elements);

        for (int i = elements.Count - 1; i >= 0; i--)
        {
            Control element = elements[i];
            if (RoomGraphics.TranslatePoint(mousePos, element) is not Point pos)
                continue;

            if (element is RoomBackgroundDisplay bgDisplay)
            {
                if (bgDisplay.DrawnBounds.Contains(pos))
                    return element;
                continue;
            }
            if (!new Rect(element.Bounds.Size).Contains(pos))
                continue;
            if (element.DataContext is Layer.LayerTilesData tilesData && IsTileLayerEmptyAt(tilesData, pos))
                continue;

            return element;
        }

        return null;
    }

    private void RoomGraphics_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        if (!e.GetCurrentPoint(RoomGraphics).Properties.IsLeftButtonPressed)
            return;

        UndertaleRoom room = vm.Room;
        Point mousePos = e.GetPosition(RoomGraphics);
        Control? element = GetRoomObjectElementAt(mousePos);
        UndertaleObject? clickedObj = element?.DataContext as UndertaleObject;

        e.Pointer.Capture(ZoomBorder);

        if (clickedObj is null)
        {
            placedTiles.Clear();

            PaintObjects(GetGridMouseCoordinates(mousePos, room, e.KeyModifiers), selectedObject, vm, true, e.KeyModifiers);
            return;
        }

        e.Handled = true;

        Layer? layer = null;
        if (room.Layers.Count > 0)
            layer = vm.GetLayerOf(clickedObj);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            IList? collection = clickedObj switch
            {
                GameObject => layer is null ? room.GameObjects : layer.InstancesData.Instances,
                Tile => layer is null ? room.Tiles : layer.AssetsData.LegacyTiles,
                SpriteInstance => layer?.AssetsData.Sprites,
                ParticleSystemInstance => layer?.AssetsData.ParticleSystems,
                _ => null
            };
            if (collection is not null)
            {
                int index = collection.IndexOf(clickedObj) + 1;
                Point pos = clickedObj switch
                {
                    IRoomObject roomObj => new(roomObj.X, roomObj.Y),
                    SpriteInstance sprInst => new(sprInst.X, sprInst.Y),
                    ParticleSystemInstance partSysInst => new(partSysInst.X, partSysInst.Y),
                    _ => new()
                };
                clickedObj = vm.AddObjectCopy(layer, clickedObj, true, index, pos);
                element = null;
            }
        }
        if (clickedObj is null)
            return;

        leftDrag = false;
        rightDrag = false;
        topDrag = false;
        bottomDrag = false;
        if (ReferenceEquals(selectedObject, clickedObj) && element is Image objElement && clickedObj is GameObject { ObjectDefinition.Sprite: not null } gameObject)
        {
            Point relativeMousePos = e.GetPosition(objElement);
            Size objSize = objElement.Bounds.Size;
            const int edgeMargin = 4;

            dragObjectOrigin = new Point(gameObject.X, gameObject.Y);
            double originX = dragObjectOrigin.X;
            double originY = dragObjectOrigin.Y;
            if (Math.Abs(relativeMousePos.X) < edgeMargin)
            {
                leftDrag = true;
                originX += (gameObject.ScaleX * (gameObject.ObjectDefinition.Sprite.Width + gameObject.SpriteXOffset));
            }
            else if (Math.Abs(objSize.Width - relativeMousePos.X) < edgeMargin)
            {
                rightDrag = true;
                originX -= (gameObject.ScaleX * -gameObject.SpriteXOffset);
            }
            if (Math.Abs(relativeMousePos.Y) < edgeMargin)
            {
                topDrag = true;
                originY += (gameObject.ScaleY * (gameObject.ObjectDefinition.Sprite.Height + gameObject.SpriteYOffset));
            }
            else if (Math.Abs(objSize.Height - relativeMousePos.Y) < edgeMargin)
            {
                bottomDrag = true;
                originY += (gameObject.ScaleY * gameObject.SpriteYOffset);
            }
            dragObjectOrigin = new Point(originX, originY);
        }

        SelectObject(clickedObj);

        if (clickedObj is GameObject || clickedObj is Tile || clickedObj is SpriteInstance || clickedObj is ParticleSystemInstance)
        {
            movingObj = clickedObj;
            vm.PushUndo(clickedObj);

            Point objPos = clickedObj switch
            {
                IRoomObject roomObj => new(roomObj.X, roomObj.Y),
                SpriteInstance sprInst => new(sprInst.X, sprInst.Y),
                ParticleSystemInstance partSysInst => new(partSysInst.X, partSysInst.Y),
                _ => new()
            };
            hotpointX = mousePos.X - objPos.X;
            hotpointY = mousePos.Y - objPos.Y;
        }
    }

    private void RoomGraphics_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        e.Pointer.Capture(null);

        placingTiles = false;
        placedTiles.Clear();

        leftDrag = false;
        rightDrag = false;
        topDrag = false;
        bottomDrag = false;

        movingObj = null;
    }

    private void PaintObjects(Point gridMouse, UndertaleObject? other, UndertaleRoomEditorViewModel vm, bool isLeftButtonPressed, KeyModifiers modifiers)
    {
        if (!isLeftButtonPressed || !modifiers.HasFlag(KeyModifiers.Alt) || other is null)
        {
            placingTiles = false;
            return;
        }

        if (placedTiles.Contains(gridMouse))
            return;

        Layer? layer = null;
        if (vm.Room.Layers.Count > 0)
            layer = vm.GetLayerOf(other);

        if (layer is not null && layer.AssetsData is null && layer.InstancesData is null)
            return;

        placedTiles.Add(gridMouse);
        placingTiles = true;

        UndertaleObject? newObj = vm.AddObjectCopy(layer, other, false, -1, gridMouse);

        if (newObj is not null)
            SelectObject(newObj);
    }

    private void RoomGraphics_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        UndertaleRoom room = vm.Room;
        Point mousePos = e.GetPosition(RoomGraphics);
        lastMousePos = mousePos;

        if (placingTiles)
        {
            PaintObjects(GetGridMouseCoordinates(mousePos, room, e.KeyModifiers), selectedObject, vm,
                         e.GetCurrentPoint(RoomGraphics).Properties.IsLeftButtonPressed, e.KeyModifiers);
            return;
        }

        if (movingObj is null)
            return;

        int tgtX = (int)(mousePos.X - hotpointX);
        int tgtY = (int)(mousePos.Y - hotpointY);
        var modifierValue = 1;
        int gridWidth = Math.Max(Convert.ToInt32(room.GridWidth), 1);
        int gridHeight = Math.Max(Convert.ToInt32(room.GridHeight), 1);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            gridWidth = 1;
            gridHeight = 1;
            modifierValue = 10;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            gridWidth /= 2;
            gridHeight /= 2;
            modifierValue = 2;
        }

        if (gridWidth > 0)
            tgtX = ((tgtX + gridWidth / 2) / gridWidth) * gridWidth;
        if (gridHeight > 0)
            tgtY = ((tgtY + gridHeight / 2) / gridHeight) * gridHeight;

        if (movingObj is GameObject gameObj)
        {
            if ((leftDrag || rightDrag || topDrag || bottomDrag) && gameObj.ObjectDefinition?.Sprite is not null)
            {
                Point objOrigin = dragObjectOrigin;
                float spriteWidth = gameObj.ObjectDefinition.Sprite.Width;
                float spriteHeight = gameObj.ObjectDefinition.Sprite.Height;
                float offsetX = -gameObj.SpriteXOffset;
                float offsetY = -gameObj.SpriteYOffset;
                if (leftDrag)
                {
                    double newXScale = Math.Ceiling(((objOrigin.X - mousePos.X) / spriteWidth) * modifierValue) / modifierValue;
                    double newXPos = objOrigin.X - (newXScale * (-offsetX + spriteWidth));
                    if (newXScale != 0 && !double.IsNaN(newXScale) && !double.IsInfinity(newXScale) &&
                                          !double.IsNaN(newXPos) && !double.IsInfinity(newXPos))
                    {
                        gameObj.ScaleX = (float)newXScale;
                        gameObj.X = (int)newXPos;
                    }
                }
                else if (rightDrag)
                {
                    double newXScale = Math.Ceiling(((mousePos.X - objOrigin.X) / spriteWidth) * modifierValue) / modifierValue;
                    double newXPos = objOrigin.X + (newXScale * offsetX);
                    if (newXScale != 0 && !double.IsNaN(newXScale) && !double.IsInfinity(newXScale) &&
                                          !double.IsNaN(newXPos) && !double.IsInfinity(newXPos))
                    {
                        gameObj.ScaleX = (float)newXScale;
                        gameObj.X = (int)newXPos;
                    }
                }
                if (topDrag)
                {
                    double newYScale = Math.Ceiling(((objOrigin.Y - mousePos.Y) / spriteHeight) * modifierValue) / modifierValue;
                    double newYPos = objOrigin.Y - (newYScale * (-offsetY + spriteHeight));
                    if (newYScale != 0 && !double.IsNaN(newYScale) && !double.IsInfinity(newYScale) &&
                                          !double.IsNaN(newYPos) && !double.IsInfinity(newYPos))
                    {
                        gameObj.ScaleY = (float)newYScale;
                        gameObj.Y = (int)newYPos;
                    }
                }
                else if (bottomDrag)
                {
                    double newYScale = Math.Ceiling(((mousePos.Y - objOrigin.Y) / spriteHeight) * modifierValue) / modifierValue;
                    double newYPos = objOrigin.Y + (newYScale * offsetY);
                    if (newYScale != 0 && !double.IsNaN(newYScale) && !double.IsInfinity(newYScale) &&
                                          !double.IsNaN(newYPos) && !double.IsInfinity(newYPos))
                    {
                        gameObj.ScaleY = (float)newYScale;
                        gameObj.Y = (int)newYPos;
                    }
                }
                return;
            }

            gameObj.X = tgtX;
            gameObj.Y = tgtY;
        }
        else if (movingObj is Tile tile)
        {
            tile.X = tgtX;
            tile.Y = tgtY;
        }
        else if (movingObj is SpriteInstance spr)
        {
            spr.X = tgtX;
            spr.Y = tgtY;
        }
        else if (movingObj is ParticleSystemInstance partSys)
        {
            partSys.X = tgtX;
            partSys.Y = tgtY;
        }
    }

    private void RoomGraphics_DragOver(object? sender, DragEventArgs e)
    {
        bool canDrop = DataContext is UndertaleRoomEditorViewModel vm && vm.CanDropAsset(ResourceDragDrop.GetResource(e));

        e.DragEffects = canDrop ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void RoomGraphics_Drop(object? sender, DragEventArgs e)
    {
        UndertaleObject? sourceItem = ResourceDragDrop.GetResource(e);
        if (DataContext is UndertaleRoomEditorViewModel vm && sourceItem is not null && vm.CanDropAsset(sourceItem))
        {
            Point mousePos = e.GetPosition(RoomGraphics);
            UndertaleObject? newObj = vm.DropAsset(sourceItem, mousePos, GetGridMouseCoordinates(mousePos, vm.Room, e.KeyModifiers));
            if (newObj is not null)
                SelectObject(newObj);

            e.DragEffects = DragDropEffects.Link;
        }
        else
            e.DragEffects = DragDropEffects.None;

        e.Handled = true;
    }

    private void RectangleTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Canvas { DataContext: Tile clickedTile } element || DataContext is not UndertaleRoomEditorViewModel vm)
            return;

        UndertaleRoom room = vm.Room;
        Point gridMouseCoordinates = GetGridMouseCoordinates(e.GetPosition(element), room, e.KeyModifiers);
        scaleOriginX = gridMouseCoordinates.X;
        scaleOriginY = gridMouseCoordinates.Y;
        clickedTile.SourceX = (int)gridMouseCoordinates.X;
        clickedTile.SourceY = (int)gridMouseCoordinates.Y;
        clickedTile.Width = (uint)room.GridWidth;
        clickedTile.Height = (uint)room.GridHeight;
    }

    private void RectangleTile_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Canvas { DataContext: Tile clickedTile } element || DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        if (!e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
            return;

        UndertaleRoom room = vm.Room;
        Point gridMouseCoordinates = GetGridMouseCoordinates(e.GetPosition(element), room, e.KeyModifiers);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && clickedTile.Tpag is not null)
        {
            double differenceX = gridMouseCoordinates.X - scaleOriginX;
            double differenceY = gridMouseCoordinates.Y - scaleOriginY;
            clickedTile.Width = (uint)Math.Clamp(Math.Abs(differenceX), 0, clickedTile.Tpag.BoundingWidth) + (uint)room.GridWidth;
            clickedTile.Height = (uint)Math.Clamp(Math.Abs(differenceY), 0, clickedTile.Tpag.BoundingHeight) + (uint)room.GridHeight;

            if (differenceX < 0)
                clickedTile.SourceX = (int)gridMouseCoordinates.X;
            else
                clickedTile.SourceX = (int)scaleOriginX;

            if (differenceY < 0)
                clickedTile.SourceY = (int)gridMouseCoordinates.Y;
            else
                clickedTile.SourceY = (int)scaleOriginY;
        }
        else
        {
            clickedTile.SourceX = (int)gridMouseCoordinates.X;
            clickedTile.SourceY = (int)gridMouseCoordinates.Y;
        }
    }

    private void SelectNewObject(UndertaleObject? newObj)
    {
        if (newObj is not null)
            SelectObject(newObj);
    }

    private void MenuItem_NewLayerInstances_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddLayer<Layer.LayerInstancesData>(LayerType.Instances, "NewInstancesLayer"));
    }

    private void MenuItem_NewLayerTiles_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddLayer<Layer.LayerTilesData>(LayerType.Tiles, "NewTilesLayer"));
    }

    private void MenuItem_NewLayerBackground_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddLayer<Layer.LayerBackgroundData>(LayerType.Background, "NewBackgroundLayer"));
    }

    private void MenuItem_NewLayerAssets_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddLayer<Layer.LayerAssetsData>(LayerType.Assets, "NewAssetsLayer"));
    }

    private void MenuItem_NewGMS2ObjectInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddGMS2ObjectInstance((sender as MenuItem)?.DataContext as Layer));
    }

    private void MenuItem_NewLegacyTile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddLegacyTile((sender as MenuItem)?.DataContext as Layer));
    }

    private void MenuItem_NewSprite_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddSprite((sender as MenuItem)?.DataContext as Layer));
    }

    private void MenuItem_NewObjectInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddObjectInstance());
    }

    private void MenuItem_NewGMS1Tile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.AddGMS1Tile());
    }

    private void MenuItem_Paste_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomEditorViewModel vm)
            SelectNewObject(vm.PasteInto((sender as MenuItem)?.DataContext as Layer));
    }

    private RenderTargetBitmap? RenderRoom(bool showGrid)
    {
        int width = (int)RoomGraphics.Bounds.Width;
        int height = (int)RoomGraphics.Bounds.Height;
        if (width <= 0 || height <= 0)
            return null;

        Control? prevFlashing = flashingElement;
        prevFlashing?.Classes.Remove("flash");
        RoomGrid.IsVisible = showGrid;
        var prevTransform = RoomGraphics.RenderTransform;
        RoomGraphics.RenderTransform = null;
        RoomGraphics.UpdateLayout();

        RenderTargetBitmap target = new(new PixelSize(width, height), new Vector(96, 96));
        target.Render(RoomGraphics);

        RoomGrid.IsVisible = true;
        RoomGraphics.RenderTransform = prevTransform;
        prevFlashing?.Classes.Add("flash");
        return target;
    }

    private RenderTargetBitmap? GetTileEditorPreview(Layer tilemap)
    {
        bool prevVisible = tilemap.IsVisible;
        tilemap.IsVisible = false;

        RenderTargetBitmap? target = RenderRoom(false);

        tilemap.IsVisible = prevVisible;
        return target;
    }

    private async void ExportAsPNG_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        if (TopLevel.GetTopLevel(this) is not TopLevel topLevel)
            return;

        var file = await vm.FileService.SaveImageFileAsync(topLevel.StorageProvider, vm.Room.Name.Content + ".png");
        if (file is null)
            return;

        try
        {
            RenderTargetBitmap? target = RenderRoom(true);
            if (target is null)
                return;

            await using var stream = await file.OpenWriteAsync();
            target.Save(stream);
        }
        catch (Exception ex)
        {
            await App.Current!.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
        }
    }

    private async void EditTiles_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        if ((sender as Control)?.DataContext is not Layer.LayerTilesData { ParentLayer: Layer layer })
            return;

        if (await vm.CanEditTiles(layer))
            await vm.EditTiles(layer, GetTileEditorPreview(layer));
    }

    private async void TileDataImport_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleRoomEditorViewModel vm)
            return;
        if ((sender as Control)?.DataContext is not Layer.LayerTilesData { ParentLayer: Layer layer })
            return;
        if (TopLevel.GetTopLevel(this) is not TopLevel topLevel)
            return;

        await vm.ImportTileData(layer, topLevel.StorageProvider);
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