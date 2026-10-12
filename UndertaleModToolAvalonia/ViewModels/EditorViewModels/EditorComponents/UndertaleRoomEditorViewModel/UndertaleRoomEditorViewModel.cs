using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel
{
    public partial class UndertaleRoomEditorViewModel(string title, UndertaleRoom room, EditorViewModel editor, IFileService fileService, ITextureCacheService textureCacheService) : EditorContentViewModel(title)
    {
        private static readonly Regex trailingNumberRegex = new(@"\d+$", RegexOptions.Compiled);
        private readonly Stack<UndertaleObject> undoStack = new();
        private readonly Type[] movableTypes = { typeof(Layer), typeof(GameObject), typeof(Tile), typeof(SpriteInstance), typeof(SequenceInstance), typeof(ParticleSystemInstance) };

        public EditorViewModel Editor { get; } = editor;
        public IFileService FileService { get; } = fileService;
        public ITextureCacheService TextureCacheService { get; } = textureCacheService;

        [ObservableProperty]
        private UndertaleRoom room = room;

        [ObservableProperty]
        private object selectedObject;

        [ObservableProperty]
        private UndertaleRoom.Layer selectedLayer;

        [ObservableProperty]
        private bool canMoveSelectedObject;

        [ObservableProperty]
        private UndertaleObject? copied;

        [ObservableProperty]
        private double roomScale = 1.0;

        [ObservableProperty]
        private double scrollX;

        [ObservableProperty]
        private double scrollY;

        public bool IsGMS2Room => Room.Flags.HasFlag(RoomEntryFlags.IsGMS2) || Room.Flags.HasFlag(RoomEntryFlags.IsGM2024_13);

        public bool IsNotGMS2Room => !IsGMS2Room;

        public string GameObjectsHeader => IsGMS2Room ? "Game objects (from all layers)" : "Game objects";

        public Type TileDefinitionType => UndertaleHelper.IsGMS2 ? typeof(UndertaleSprite) : typeof(UndertaleBackground);

        public AnimationSpeedType[] AnimationSpeedTypes { get; } = 
            Enum.GetValues<AnimationSpeedType>();

        public void SetupRoom()
        {
            if (Settings.Instance.GridWidthEnabled)
                Room.GridWidth = Settings.Instance.GlobalGridWidth;
            if (Settings.Instance.GridHeightEnabled)
                Room.GridHeight = Settings.Instance.GlobalGridHeight;

            if (Settings.Instance.GridThicknessEnabled)
                Room.GridThicknessPx = Settings.Instance.GlobalGridThickness;
            else
                Room.GridThicknessPx = 1;
            Room.SetupRoom(!Settings.Instance.GridWidthEnabled, !Settings.Instance.GridHeightEnabled);

            if (Room.Layers.Count > 0 && !Room.CheckLayersDepthOrder())
                Room.RearrangeLayers();
        }

        partial void OnSelectedObjectChanged(object value)
        {
            if (value is UndertaleRoom.Layer layer)
                SelectedLayer = layer;

            CanMoveSelectedObject = value is not null && movableTypes.Contains(value.GetType());
        }

        [RelayCommand]
        private void Copy(UndertaleObject obj)
        {
            if (obj is null) return;

            Copied = obj;
        }

        [RelayCommand]
        private void DeleteItem(UndertaleObject obj)
        {
            if (obj is null) return;

            bool clearEditor = ReferenceEquals(obj, SelectedObject);

            if (obj is Background bg)
            {
                bg.Enabled = false;
                bg.BackgroundDefinition = null;
            }
            else if (obj is View view)
            {
                view.Enabled = false;
            }
            else if (obj is Tile tile)
            {
                if (UndertaleHelper.IsGMS2)
                {
                    foreach (var layer in Room.Layers)
                        if (layer.AssetsData != null)
                            layer.AssetsData.LegacyTiles.Remove(tile);
                }

                Room.Tiles.Remove(tile);
            }
            else if (obj is GameObject gameObj)
            {
                if (UndertaleHelper.IsGMS2)
                {
                    foreach (var layer in Room.Layers)
                        if (layer.InstancesData != null)
                            layer.InstancesData.Instances.Remove(gameObj);
                }

                Room.GameObjects.Remove(gameObj);
            }
            else if (obj is SpriteInstance sprInst)
            {
                foreach (var layer in Room.Layers)
                    if (layer.AssetsData != null)
                        layer.AssetsData.Sprites.Remove(sprInst);
            }
            else if (obj is ParticleSystemInstance partSysInst)
            {
                foreach (var layer in Room.Layers)
                    if (layer.AssetsData != null)
                        layer.AssetsData.ParticleSystems.Remove(partSysInst);
            }
            else if (obj is Layer layer)
            {
                if (layer.InstancesData != null)
                    foreach (var go in layer.InstancesData.Instances)
                        Room.GameObjects.Remove(go);

                Room.Layers.Remove(layer);

                if (layer.LayerType == LayerType.Background)
                    Room.UpdateBGColorLayer();
            }

            if (clearEditor)
                SelectedObject = null;
        }

        [RelayCommand]
        private async Task AutoSizeTiles(Layer.LayerTilesData data)
        {
            if (data is null) return;

            if (data.Background is null)
            {
                await App.Current!.ShowMessage("The layer must have a tileset set!");
                return;
            }
            data.TilesX = (uint)Math.Ceiling(
                Convert.ToDouble(Room.Width) / Convert.ToDouble(data.Background.GMS2TileWidth)
            );
            data.TilesY = (uint)Math.Ceiling(
                Convert.ToDouble(Room.Height) / Convert.ToDouble(data.Background.GMS2TileHeight)
            );
        }

        public async Task<bool> MoveItem(UndertaleObject obj, int dist, bool isInAllGameObjectsList)
        {
            if (obj is Layer)
            {
                await App.Current!.ShowError("Layers don't support this feature currently, change the layer depths instead.");
                return false;
            }
            if (obj is View)
            {
                await App.Current!.ShowError("Views don't support this feature.");
                return false;
            }
            if (obj is Background)
            {
                await App.Current!.ShowError("Backgrounds don't support this feature.");
                return false;
            }

            Layer? layer = null;
            if (Room.Layers.Count > 0)
                layer = GetLayerOf(obj);

            IList? list = obj switch
            {
                Tile => layer is null ? Room.Tiles : layer.AssetsData.LegacyTiles,
                GameObject => isInAllGameObjectsList ? Room.GameObjects : (layer is null ? Room.GameObjects : layer.InstancesData.Instances),
                SpriteInstance => layer?.AssetsData.Sprites,
                ParticleSystemInstance => layer?.AssetsData.ParticleSystems,
                _ => null
            };
            if (list is null)
            {
                await App.Current!.ShowError("Can't change the object position - no list for the selected object was found.");
                return false;
            }

            int index = list.IndexOf(obj);
            int newIndex = Math.Clamp(index + dist, 0, list.Count - 1);
            if (newIndex != index)
            {
                var prevObj = list[newIndex];
                list[newIndex] = obj;
                list[index] = prevObj;
            }

            return true;
        }

        [RelayCommand]
        private void Undo()
        {
            if (!undoStack.Any()) return;

            var undoObject = undoStack.Pop();
            if (undoObject is GameObject undoGameObject && SelectedObject is GameObject gameObjToChange)
            {
                if (gameObjToChange.InstanceID == undoGameObject.InstanceID)
                {
                    gameObjToChange.X = undoGameObject.X;
                    gameObjToChange.Y = undoGameObject.Y;
                    gameObjToChange.ScaleX = undoGameObject.ScaleX;
                    gameObjToChange.ScaleY = undoGameObject.ScaleY;
                }
            }
            if (undoObject is Tile undoTile && SelectedObject is Tile tileToChange)
            {
                if (tileToChange.InstanceID == undoTile.InstanceID)
                {
                    tileToChange.X = undoTile.X;
                    tileToChange.Y = undoTile.Y;
                }
            }
            Room.SetupRoom(false, false);
        }

        public UndertaleObject? Paste(Point mousePos, bool shiftPressed = false)
        {
            if (Copied is null)
                return null;

            Layer? layer = null;
            if (Room.Layers.Count > 0)
            {
                layer = SelectedObject switch
                {
                    Tile tile => GetLayerOf(tile),
                    GameObject gameObj => GetLayerOf(gameObj),
                    SpriteInstance spr => GetLayerOf(spr),
                    Layer l => l,
                    _ => null
                };
            }

            if (UndertaleHelper.IsGMS2 && layer == null)
            {
                _ = App.Current!.ShowError("Please select a layer.");
                return null;
            }

            if (!shiftPressed)
            {
                int gridWidth = Math.Max(Convert.ToInt32(Room.GridWidth), 1);
                int gridHeight = Math.Max(Convert.ToInt32(Room.GridHeight), 1);
                mousePos = new Point(Math.Floor(mousePos.X / gridWidth) * gridWidth, Math.Floor(mousePos.Y / gridHeight) * gridHeight);
            }

            return AddObjectCopy(layer, Copied, true, -1, mousePos);
        }

        public UndertaleObject? PasteInto(Layer? layer)
        {
            if (Copied is null)
                return null;

            return AddObjectCopy(UndertaleHelper.IsGMS2 ? layer : null, Copied, true);
        }

        public GameObject AddObjectInstance()
        {
            var newObject = new GameObject { InstanceID = AppConstants.Data!.GeneralInfo.LastObj++ };
            Room.GameObjects.Add(newObject);

            return newObject;
        }

        public GameObject? AddGMS2ObjectInstance(Layer? layer)
        {
            GameObject newObject = new() { InstanceID = AppConstants.Data!.GeneralInfo.LastObj++ };
            Room.GameObjects.Add(newObject);

            if (layer is null)
                return null;

            layer.InstancesData.Instances.Add(newObject);
            return newObject;
        }

        public Tile? AddLegacyTile(Layer? layer)
        {
            if (layer is null)
                return null;

            layer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();
            layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();

            var tile = new Tile { InstanceID = AppConstants.Data!.GeneralInfo.LastTile++ };
            tile.spriteMode = true;
            layer.AssetsData.LegacyTiles.Add(tile);

            return tile;
        }

        public SpriteInstance? AddSprite(Layer? layer)
        {
            if (layer is null)
                return null;

            layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();

            SpriteInstance spriteInstance = new()
            {
                Name = SpriteInstance.GenerateRandomName(AppConstants.Data)
            };

            layer.AssetsData.Sprites.Add(spriteInstance);

            return spriteInstance;
        }

        public Tile AddGMS1Tile()
        {
            var tile = new Tile { InstanceID = AppConstants.Data!.GeneralInfo.LastTile++ };
            Room.Tiles.Add(tile);

            return tile;
        }

        public void PushUndo(UndertaleObject obj)
        {
            if (obj is GameObject gameObj)
            {
                undoStack.Push(new GameObject()
                {
                    X = gameObj.X,
                    Y = gameObj.Y,
                    ObjectDefinition = gameObj.ObjectDefinition,
                    InstanceID = gameObj.InstanceID,
                    CreationCode = gameObj.CreationCode,
                    ScaleX = gameObj.ScaleX,
                    ScaleY = gameObj.ScaleY,
                    Color = gameObj.Color,
                    Rotation = gameObj.Rotation,
                    PreCreateCode = gameObj.PreCreateCode
                });
            }
            else if (obj is Tile tile)
            {
                undoStack.Push(new Tile()
                {
                    X = tile.X,
                    Y = tile.Y,
                    spriteMode = tile.spriteMode,
                    ObjectDefinition = tile.ObjectDefinition,
                    SourceX = tile.SourceX,
                    SourceY = tile.SourceY,
                    Width = tile.Width,
                    Height = tile.Height,
                    TileDepth = tile.TileDepth,
                    InstanceID = tile.InstanceID,
                    ScaleX = tile.ScaleX,
                    ScaleY = tile.ScaleY,
                    Color = tile.Color
                });
            }
            else if (obj is SpriteInstance sprInst)
            {
                undoStack.Push(new SpriteInstance
                {
                    Name = sprInst.Name,
                    Sprite = sprInst.Sprite,
                    X = sprInst.X,
                    Y = sprInst.Y,
                    ScaleX = sprInst.ScaleX,
                    ScaleY = sprInst.ScaleY,
                    Color = sprInst.Color,
                    AnimationSpeed = sprInst.AnimationSpeed,
                    AnimationSpeedType = sprInst.AnimationSpeedType,
                    FrameIndex = sprInst.FrameIndex,
                    Rotation = sprInst.Rotation
                });
            }
            else if (obj is ParticleSystemInstance partSysInst)
            {
                undoStack.Push(new ParticleSystemInstance
                {
                    Name = partSysInst.Name,
                    ParticleSystem = partSysInst.ParticleSystem,
                    X = partSysInst.X,
                    Y = partSysInst.Y,
                    ScaleX = partSysInst.ScaleX,
                    ScaleY = partSysInst.ScaleY,
                    Color = partSysInst.Color,
                    Rotation = partSysInst.Rotation,
                    InstanceID = partSysInst.InstanceID
                });
            }
        }

        public UndertaleObject? AddObjectCopy(Layer? layer, UndertaleObject obj, bool showErrors, int insertIndex = -1, Point pos = new())
        {
            if (obj is null)
                return null;

            UndertaleObject? newObj = null;

            if (obj is Tile tile)
            {
                if (layer != null && layer.AssetsData == null)
                {
                    _ = App.Current!.ShowError("Please select an assets layer.");
                    return null;
                }

                var newTile = new Tile
                {
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    spriteMode = tile.spriteMode,
                    ObjectDefinition = tile.ObjectDefinition,
                    SpriteDefinition = tile.SpriteDefinition,
                    SourceX = tile.SourceX,
                    SourceY = tile.SourceY,
                    Width = tile.Width,
                    Height = tile.Height,
                    TileDepth = tile.TileDepth,
                    InstanceID = AppConstants.Data!.GeneralInfo.LastTile++,
                    ScaleX = tile.ScaleX,
                    ScaleY = tile.ScaleY,
                    Color = tile.Color
                };

                newObj = newTile;

                int index;
                if (layer != null)
                {
                    index = insertIndex > -1 ? insertIndex : layer.AssetsData.LegacyTiles.Count;
                    layer.AssetsData.LegacyTiles.Insert(index, newTile);
                }
                else
                {
                    index = insertIndex > -1 ? insertIndex : Room.Tiles.Count;
                    Room.Tiles.Insert(index, newTile);
                }

                SetupRoom();
            }
            else if (obj is GameObject gameObj)
            {
                if (layer != null && layer.InstancesData == null)
                {
                    _ = App.Current!.ShowError("Please select an instance layer.");
                    return null;
                }

                var newGameObj = new GameObject
                {
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    ObjectDefinition = gameObj.ObjectDefinition,
                    InstanceID = AppConstants.Data!.GeneralInfo.LastObj++,
                    CreationCode = gameObj.CreationCode,
                    ScaleX = gameObj.ScaleX,
                    ScaleY = gameObj.ScaleY,
                    Color = gameObj.Color,
                    Rotation = gameObj.Rotation,
                    PreCreateCode = gameObj.PreCreateCode,
                    ImageSpeed = gameObj.ImageSpeed,
                    ImageIndex = gameObj.ImageIndex
                };

                newObj = newGameObj;

                int index = insertIndex > -1 ? Math.Min(insertIndex, Room.GameObjects.Count) : Room.GameObjects.Count;
                Room.GameObjects.Insert(index, newGameObj);
                if (layer != null)
                {
                    index = insertIndex > -1 ? insertIndex : layer.InstancesData.Instances.Count;
                    layer.InstancesData.Instances.Insert(index, newGameObj);
                }
            }
            else if (obj is SpriteInstance sprInst)
            {
                if (layer != null && layer.AssetsData == null)
                {
                    _ = App.Current!.ShowError("Please select an assets layer.");
                    return null;
                }

                var newSprInst = new SpriteInstance
                {
                    Name = SpriteInstance.GenerateRandomName(AppConstants.Data),
                    Sprite = sprInst.Sprite,
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    ScaleX = sprInst.ScaleX,
                    ScaleY = sprInst.ScaleY,
                    Color = sprInst.Color,
                    AnimationSpeed = sprInst.AnimationSpeed,
                    AnimationSpeedType = sprInst.AnimationSpeedType,
                    FrameIndex = sprInst.FrameIndex,
                    Rotation = sprInst.Rotation
                };

                newObj = newSprInst;

                if (layer != null)
                {
                    int index = insertIndex > -1 ? insertIndex : layer.AssetsData.Sprites.Count;
                    layer.AssetsData.Sprites.Insert(index, newSprInst);
                }
            }
            else if (obj is ParticleSystemInstance partSysInst)
            {
                if (layer != null && layer.AssetsData == null)
                {
                    _ = App.Current!.ShowError("Please select an assets layer.");
                    return null;
                }

                var newPartSysInst = new ParticleSystemInstance
                {
                    Name = ParticleSystemInstance.GenerateRandomName(AppConstants.Data),
                    InstanceID = ++AppConstants.Data!.LastParticleSystemInstanceID,
                    ParticleSystem = partSysInst.ParticleSystem,
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    ScaleX = partSysInst.ScaleX,
                    ScaleY = partSysInst.ScaleY,
                    Color = partSysInst.Color,
                    Rotation = partSysInst.Rotation
                };

                newObj = newPartSysInst;

                if (layer != null)
                {
                    int index = insertIndex > -1 ? insertIndex : layer.AssetsData.ParticleSystems.Count;
                    layer.AssetsData.ParticleSystems.Insert(index, newPartSysInst);
                }
            }

            return newObj;
        }

        public Layer AddLayer<T>(LayerType type, string name) where T : Layer.LayerData, new()
        {
            var data = AppConstants.Data!;
            uint largest_layerid = 0;

            foreach (UndertaleRoom room in data.Rooms)
            {
                if (room is null)
                    continue;
                foreach (Layer roomLayer in room.Layers)
                {
                    if (roomLayer.LayerId > largest_layerid)
                        largest_layerid = roomLayer.LayerId;
                }
            }

            long layerDepth = 0;
            if (Room.Layers.Count > 0)
            {
                layerDepth = Room.Layers.Select(l => l.LayerDepth).Max();
                if (layerDepth + 100 > Int32.MaxValue)
                {
                    if (layerDepth + 1 > Int32.MaxValue)
                    {
                        layerDepth -= 1;
                        _ = App.Current!.ShowWarning("Warning - the maximum layer depth is reached.\nYou probably should change the depth of the new layer.");
                    }
                    else
                        layerDepth += 1;
                }
                else
                    layerDepth += 100;
            }

            string? baseName = null;
            int nameNum = 0;
            while (Room.Layers.Any(l => l.LayerName.Content == name))
            {
                if (baseName is null)
                {
                    Match numMatch = trailingNumberRegex.Match(name);

                    if (numMatch.Success)
                    {
                        baseName = name[..^numMatch.Length];
                        nameNum = Int32.Parse(numMatch.Groups[0].Value) + 1;
                    }
                    else
                    {
                        baseName = name;
                        nameNum = 1;
                    }
                }
                else
                    nameNum++;
                name = baseName + nameNum;
            }

            Layer layer = new()
            {
                LayerName = data.Strings.MakeString(name),
                LayerId = largest_layerid + 1,
                LayerType = type,
                LayerDepth = (int)layerDepth,
                Data = new T()
            };
            Room.Layers.Add(layer);
            Room.UpdateBGColorLayer();

            if (Room.Layers.Count > 1)
            {
                foreach (Layer l in Room.Layers)
                    l.UpdateZIndex();
            }
            layer.ParentRoom = Room;

            if (layer.LayerType == LayerType.Assets)
            {
                layer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();
                layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();
                layer.AssetsData.Sequences ??= new UndertalePointerList<SequenceInstance>();

                if (!data.IsVersionAtLeast(2, 3, 2))
                    layer.AssetsData.NineSlices ??= new UndertalePointerList<SpriteInstance>();
                if (data.IsNonLTSVersionAtLeast(2023, 2))
                    layer.AssetsData.ParticleSystems ??= new UndertalePointerList<ParticleSystemInstance>();
                if (data.IsVersionAtLeast(2024, 6))
                    layer.AssetsData.TextItems ??= new UndertalePointerList<TextItemInstance>();
            }
            else if (layer.LayerType == LayerType.Tiles)
            {
                layer.TilesData.TileData ??= Array.Empty<uint[]>();
            }

            return layer;
        }

        public bool CanDropAsset(UndertaleObject? sourceItem)
        {
            return sourceItem is not null
                   && ((!UndertaleHelper.IsGMS2 && sourceItem is UndertaleBackground)
                       || sourceItem is UndertaleGameObject
                       || (UndertaleHelper.IsGMS2 && sourceItem is UndertaleSprite));
        }

        public UndertaleObject? DropAsset(UndertaleObject sourceItem, Point mousePos, Point snappedPos)
        {
            Layer? layer = null;
            if (Room.Layers.Count > 0)
            {
                layer = SelectedObject switch
                {
                    GameObject gameObj => GetLayerOf(gameObj),
                    SpriteInstance sprInst => GetLayerOf(sprInst),
                    Layer l => l,
                    _ => null
                };
            }

            if (sourceItem is UndertaleBackground droppedBG)
            {
                Background? firstBG = Room.Backgrounds.FirstOrDefault(x => x.BackgroundDefinition is null);
                if (firstBG is not null)
                {
                    firstBG.Enabled = true;
                    firstBG.BackgroundDefinition = droppedBG;

                    return firstBG;
                }
                else
                    _ = App.Current!.ShowError("No empty room backgrounds.");
            }
            else if (sourceItem is UndertaleGameObject droppedObj)
            {
                if (UndertaleHelper.IsGMS2 && (layer == null || layer.InstancesData == null))
                {
                    layer = Room.Layers.FirstOrDefault(x => x.InstancesData != null)
                            ?? AddLayer<Layer.LayerInstancesData>(LayerType.Instances, "Instances");
                }

                GameObject obj = new()
                {
                    X = Convert.ToInt32(snappedPos.X),
                    Y = Convert.ToInt32(snappedPos.Y),
                    ObjectDefinition = droppedObj,
                    InstanceID = AppConstants.Data!.GeneralInfo.LastObj++
                };
                Room.GameObjects.Add(obj);
                if (layer != null)
                    layer.InstancesData.Instances.Add(obj);

                return obj;
            }
            else if (sourceItem is UndertaleSprite droppedSprite)
            {
                if (UndertaleHelper.IsGMS2 && layer == null)
                {
                    _ = App.Current!.ShowError("Please select a layer.");
                    return null;
                }
                if (layer == null || layer.AssetsData == null)
                {
                    _ = App.Current!.ShowError("Please select an assets layer.");
                    return null;
                }

                SpriteInstance sprInst = new()
                {
                    X = (int)mousePos.X,
                    Y = (int)mousePos.Y,
                    Name = SpriteInstance.GenerateRandomName(AppConstants.Data),
                    Sprite = droppedSprite
                };

                layer.AssetsData.Sprites.Add(sprInst);

                return sprInst;
            }

            return null;
        }

        public Layer? GetLayerOf(UndertaleObject obj)
        {
            return obj switch
            {
                GameObject gameObj => Room.Layers.FirstOrDefault(x => x.InstancesData?.Instances.Contains(gameObj) == true),
                Tile tile => Room.Layers.FirstOrDefault(x => x.AssetsData?.LegacyTiles?.Contains(tile) == true),
                SpriteInstance spr => Room.Layers.FirstOrDefault(x => x.AssetsData?.Sprites?.Contains(spr) == true),
                ParticleSystemInstance partSys => Room.Layers.FirstOrDefault(x => x.AssetsData?.ParticleSystems?.Contains(partSys) == true),
                _ => null
            };
        }
    }
}
