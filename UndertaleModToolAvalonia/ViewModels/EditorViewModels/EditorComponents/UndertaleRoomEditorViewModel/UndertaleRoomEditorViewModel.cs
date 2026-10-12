using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private Layer? GetLayerOf(UndertaleObject obj)
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
