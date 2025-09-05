using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleFontEditorViewModel
{
    public partial class UndertaleFontEditorViewModel : EditorContentViewModel
    {
        [ObservableProperty]
        private UndertaleFont undertaleFont;

        [ObservableProperty]
        EditorViewModel editor;

        [ObservableProperty]
        ITextureCacheService textureCacheService;

        [ObservableProperty]
        IFileService fileService;

        IDialogService dialogService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GlyphsLabelText))]
        private UndertaleFont.Glyph? selectedGlyph;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGlyphViewVisible))]
        private bool isKerningViewVisible = false;

        public string EditRectangleButtonText =>
            SelectedGlyph?.SourceWidth == 0 || SelectedGlyph?.SourceHeight == 0
            ? "Select a region of an empty glyph"
            : "Edit a selected glyph rectangle";

        partial void OnIsKerningViewVisibleChanged(bool value)
        {
            // This will update the label text when we switch views
            OnPropertyChanged(nameof(GlyphsLabelText));
        }

        public bool IsGlyphViewVisible => !IsKerningViewVisible;

        public string GlyphsLabelText
        {
            get
            {
                if (IsKerningViewVisible && SelectedGlyph != null)
                {
                    char ch = Convert.ToChar(SelectedGlyph.Character);
                    return $"Kerning of glyph '{ch}' (code - {SelectedGlyph.Character}):";
                }
                return "Glyphs:";
            }
        }

        public UndertaleFontEditorViewModel(string title, UndertaleFont undertaleFont, EditorViewModel editor, ITextureCacheService textureCacheService, IFileService fileService, IDialogService dialogService) : base(title)
        {
            this.UndertaleFont = undertaleFont;
            this.Editor = editor;
            this.TextureCacheService = textureCacheService;
            this.FileService = fileService;
            this.dialogService = dialogService;
        }

        [RelayCommand]
        private async Task SortGlyphs()
        {
            // There is no way to sort an ObservableCollection in place so we have to do this
            var copy = UndertaleFont.Glyphs.ToList();
            copy.Sort((x, y) => x.Character.CompareTo(y.Character));
            UndertaleFont.Glyphs.Clear();
            foreach (var glyph in copy)
                UndertaleFont.Glyphs.Add(glyph);

            await App.Current!.ShowMessage("The glyphs were sorted successfully.");
        }

        [RelayCommand]
        private async Task UpdateRange()
        {
            var characters = UndertaleFont.Glyphs.Select(x => x.Character);
            if (characters.Any())
            {
                UndertaleFont.RangeStart = characters.Min();
                UndertaleFont.RangeEnd = characters.Max();
                await App.Current!.ShowMessage("The range was updated successfully.");
            }
            else
            {
                await App.Current!.ShowWarning("There are no glyphs to calculate a range from.");
            }
        }

        [RelayCommand]
        private async Task CreateGlyph()
        {
            var lastGlyph = UndertaleFont.Glyphs.LastOrDefault();
            if (lastGlyph != null && (lastGlyph.SourceWidth == 0 || lastGlyph.SourceHeight == 0))
            {
                await App.Current!.ShowWarning("The last glyph has zero size.\n" +
                                               "You can use the button on the left to fix that.");
                return;
            }
            UndertaleFont.Glyph glyph = new UndertaleFont.Glyph();
            UndertaleFont.Glyphs.Add(glyph);
            SelectedGlyph = glyph;
        }

        [RelayCommand]
        private void EditKerning(UndertaleFont.Glyph? glyph)
        {
            if (glyph == null) return;
            SelectedGlyph = glyph;
            IsKerningViewVisible = true;
        }

        [RelayCommand]
        private void HideKerningEditor()
        {
            IsKerningViewVisible = false;
        }

        [RelayCommand]
        private async Task EditGlyphRectangle(Window owner)
        {
            var parameters = new UndertaleFontParameters(UndertaleFont, SelectedGlyph);

            var result = await dialogService.ShowDialogAsync<EditGlyphRectangleViewModel, UndertaleFontParameters, UndertaleFont.Glyph>(owner, parameters);

            SelectedGlyph = UndertaleFont.Glyphs.FirstOrDefault(x => x.SourceX == result?.SourceX
                                                       && x.SourceY == result.SourceY
                                                       && x.SourceWidth == result.SourceWidth
                                                       && x.SourceHeight == result.SourceHeight
                                                       && x.Character == result.Character
                                                       && x.Shift == result.Shift
                                                       && x.Offset == result.Offset)!;//UndertaleFont.Glyphs.FirstOrDefault(x => x == SelectedGlyph);
        }
    }
}
