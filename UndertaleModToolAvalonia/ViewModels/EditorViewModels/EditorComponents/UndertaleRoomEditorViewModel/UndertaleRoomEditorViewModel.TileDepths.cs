using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleRoomEditorViewModel
{
    public partial class TileDepthGroup(int depth) : ObservableObject
    {
        public int Depth { get; } = depth;

        public ObservableCollection<UndertaleRoom.Tile> Tiles { get; } = new();

        [ObservableProperty]
        private bool isShown = true;
    }

    public partial class UndertaleRoomEditorViewModel
    {
        private ObservableCollection<TileDepthGroup>? tileDepthGroups;
        private readonly HashSet<int> hiddenTileDepths = new();
        private readonly Dictionary<UndertaleRoom.Tile, int> trackedTileDepths = new();

        public event Action? TileVisibilityChanged;

        public ObservableCollection<TileDepthGroup> TileDepthGroups
        {
            get
            {
                if (tileDepthGroups is null)
                {
                    tileDepthGroups = new();
                    foreach (UndertaleRoom.Tile tile in Room.Tiles)
                        TrackTile(tile);
                    Room.Tiles.CollectionChanged += RoomTiles_CollectionChanged;
                }
                return tileDepthGroups;
            }
        }

        public bool IsTileDepthShown(int depth) => !hiddenTileDepths.Contains(depth);

        public TileDepthGroup? TileDepthGroupOf(UndertaleRoom.Tile tile) =>
            TileDepthGroups.FirstOrDefault(group => group.Tiles.Contains(tile));

        private void RoomTiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (UndertaleRoom.Tile tile in trackedTileDepths.Keys.ToList())
                    UntrackTile(tile);
                foreach (UndertaleRoom.Tile tile in Room.Tiles)
                    TrackTile(tile);
            }
            else
            {
                foreach (UndertaleRoom.Tile tile in e.OldItems?.OfType<UndertaleRoom.Tile>() ?? [])
                    UntrackTile(tile);
                foreach (UndertaleRoom.Tile tile in e.NewItems?.OfType<UndertaleRoom.Tile>() ?? [])
                    TrackTile(tile);
            }
            TileVisibilityChanged?.Invoke();
        }

        private void TrackTile(UndertaleRoom.Tile tile)
        {
            if (trackedTileDepths.ContainsKey(tile))
                return;
            trackedTileDepths[tile] = tile.TileDepth;

            TileDepthGroup? group = tileDepthGroups!.FirstOrDefault(g => g.Depth == tile.TileDepth);
            if (group is null)
            {
                group = new TileDepthGroup(tile.TileDepth) { IsShown = IsTileDepthShown(tile.TileDepth) };
                group.PropertyChanged += TileDepthGroup_PropertyChanged;
                int groupIndex = 0;
                while (groupIndex < tileDepthGroups!.Count && tileDepthGroups[groupIndex].Depth < group.Depth)
                    groupIndex++;
                tileDepthGroups.Insert(groupIndex, group);
            }

            int roomIndex = Room.Tiles.IndexOf(tile);
            int tileIndex = group.Tiles.Count(t => Room.Tiles.IndexOf(t) < roomIndex);
            group.Tiles.Insert(tileIndex, tile);
            tile.PropertyChanged += Tile_PropertyChanged;
        }

        private void UntrackTile(UndertaleRoom.Tile tile)
        {
            if (!trackedTileDepths.Remove(tile, out int depth))
                return;
            tile.PropertyChanged -= Tile_PropertyChanged;

            TileDepthGroup? group = tileDepthGroups!.FirstOrDefault(g => g.Depth == depth);
            if (group is null)
                return;
            group.Tiles.Remove(tile);
            if (group.Tiles.Count == 0)
            {
                group.PropertyChanged -= TileDepthGroup_PropertyChanged;
                tileDepthGroups.Remove(group);
            }
        }

        private void Tile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UndertaleRoom.Tile.TileDepth) || sender is not UndertaleRoom.Tile tile)
                return;
            UntrackTile(tile);
            TrackTile(tile);
            TileVisibilityChanged?.Invoke();
        }

        private void TileDepthGroup_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(TileDepthGroup.IsShown) || sender is not TileDepthGroup group)
                return;
            if (group.IsShown)
                hiddenTileDepths.Remove(group.Depth);
            else
                hiddenTileDepths.Add(group.Depth);
            TileVisibilityChanged?.Invoke();
        }
    }
}
