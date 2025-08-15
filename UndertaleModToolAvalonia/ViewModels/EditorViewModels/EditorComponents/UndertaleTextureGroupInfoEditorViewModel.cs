using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleTextureGroupInfoEditorViewModel(string title, UndertaleTextureGroupInfo undertaleTextureGroupInfo) : EditorContentViewModel(title)
    {
        [ObservableProperty]
        private UndertaleTextureGroupInfo undertaleTextureGroupInfo = undertaleTextureGroupInfo;

        [RelayCommand]
        private void AddTexturePage()
        {
            UndertaleTextureGroupInfo.TexturePages.Add(new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>());
        }

        [RelayCommand]
        private void AddSprite()
        {
            UndertaleTextureGroupInfo.Sprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>());
        }

        [RelayCommand]
        private void AddSpineSprite()
        {
            UndertaleTextureGroupInfo.SpineSprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>());
        }

        [RelayCommand]
        private void AddFont()
        {
            UndertaleTextureGroupInfo.Fonts.Add(new UndertaleResourceById<UndertaleFont, UndertaleChunkFONT>());
        }

        [RelayCommand]
        private void AddTileSet()
        {
            UndertaleTextureGroupInfo.Tilesets.Add(new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>());
        }
    }
}
