using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;
using static UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel.RectangleHelper;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel
{
    public class RectangleHelper
    {
        public enum HitType
        {
            None, Body, L, R, T, B, UL, UR, LR, LL
        };
        private const int rectEdgeWidth = 1;

        public static HitType GetHitType(UndertaleFont.Glyph glyph, Rect rect, Point point)
        {
            if (glyph == null)
                return HitType.None;

            ushort left = glyph.SourceX;
            ushort top = glyph.SourceY;
            int right = left + glyph.SourceWidth;
            int bottom = top + glyph.SourceHeight;
            if (point.X < left || point.X > right
                || point.Y < top || point.Y > bottom)
                return HitType.None;

            if (point.X - left < rectEdgeWidth)
            {
                // Left edge.
                if (point.Y - top < rectEdgeWidth)
                    return HitType.UL;
                if (bottom - point.Y < rectEdgeWidth)
                    return HitType.LL;
                return HitType.L;
            }
            else if (right - point.X < rectEdgeWidth)
            {
                // Right edge.
                if (point.Y - top < rectEdgeWidth)
                    return HitType.UR;
                if (bottom - point.Y < rectEdgeWidth)
                    return HitType.LR;
                return HitType.R;
            }
            if (point.Y - top < rectEdgeWidth)
                return HitType.T;
            if (bottom - point.Y < rectEdgeWidth)
                return HitType.B;

            return HitType.Body;
        }

        public static Cursor GetCursorForType(HitType hitType)
        {
            return new Cursor(hitType switch
            {
                HitType.None => StandardCursorType.Arrow,
                HitType.Body => StandardCursorType.SizeAll,
                HitType.UL => StandardCursorType.TopLeftCorner,
                HitType.LR => StandardCursorType.BottomRightCorner,
                HitType.LL => StandardCursorType.BottomLeftCorner,
                HitType.UR => StandardCursorType.TopRightCorner,
                HitType.T => StandardCursorType.TopSide,
                HitType.B => StandardCursorType.BottomSide,
                HitType.L => StandardCursorType.LeftSide,
                HitType.R => StandardCursorType.RightSide,
                _ => StandardCursorType.Arrow
            });
        }
    }

    public partial class EditGlyphRectangleViewModel(EditorViewModel editor, ITextureCacheService textureCacheService, IFileService fileService) : ViewModelBase, IInitializable<UndertaleFontParameters>, IDialogViewModel<UndertaleFont.Glyph>
    {
        [ObservableProperty]
        private EditorViewModel _editor = editor;

        [ObservableProperty]
        private ITextureCacheService _textureCacheService = textureCacheService;

        [ObservableProperty]
        private IFileService _fileService = fileService;

        [ObservableProperty]
        private UndertaleFont? _font;

        [ObservableProperty]
        private ObservableCollection<GlyphViewModel>? _glyphs;

        [ObservableProperty]
        private GlyphViewModel? _selectedGlyph;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private Point _initPoint;

        [ObservableProperty]
        private HitType _initType;

        [ObservableProperty]
        private Rect _selectedRect;

        [ObservableProperty]
        private short _initShift;

        public async Task<bool> InitializeAsync(UndertaleFontParameters parameters)
        {
            if (parameters.font is null || parameters.selectedGlyph is null)
            {
                return false;
            }

            Font = parameters.font;
            Glyphs = new ObservableCollection<GlyphViewModel>(Font.Glyphs.Select(g => new GlyphViewModel(g.Clone())));
            SelectedGlyph = Glyphs.FirstOrDefault(x => x.SourceX == parameters.selectedGlyph.SourceX
                                                       && x.SourceY == parameters.selectedGlyph.SourceY
                                                       && x.SourceWidth == parameters.selectedGlyph.SourceWidth
                                                       && x.SourceHeight == parameters.selectedGlyph.SourceHeight
                                                       && x.Character == parameters.selectedGlyph.Character
                                                       && x.Shift == parameters.selectedGlyph.Shift
                                                       && x.Offset == parameters.selectedGlyph.Offset)!;
            if (SelectedGlyph is null)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await App.Current!.ShowError("Cannot find the selected glyph.");
                });
                return false;
            }

            return true;
        }

        partial void OnSelectedGlyphChanged(GlyphViewModel? oldValue, GlyphViewModel newValue)
        {
            if (oldValue is not null)
            {
                oldValue.IsSelected = false;
            }
            if (newValue is not null)
            {
                newValue.IsSelected = true;
            }
        }
        
        [RelayCommand]
        private async Task Help()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await App.Current!.ShowMessage("1) Double-click an inactive rectangle to select it.\n" +
                                               "2) You can move the selected rectangle with the arrow keys (held Shift - resize).\n" +
                                               "3) Drag mouse on desired region if it's an empty glyph.", "Help");
            });
        }

        public string Title => string.Empty;
        public UndertaleFont.Glyph? Result { get; set; }
        public void FinalizeResult(bool success)
        {
            if (Font != null)
                for (int i = 0; i < Font.Glyphs.Count; i++)
                    Font.Glyphs[i] = Glyphs?[i].Model;

            Result = SelectedGlyph?.Model;
            WeakReferenceMessenger.Default.Send(new CloseDialogMessage());
        }
    }
}
