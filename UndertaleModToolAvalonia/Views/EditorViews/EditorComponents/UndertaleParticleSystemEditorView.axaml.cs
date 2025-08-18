using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleParticleSystemEditorView : UserControl
{
    public UndertaleParticleSystemEditorView()
    {
        InitializeComponent();
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleParticleSystemEditorViewModel vm)
            return;
        vm.UndertaleParticleSystem.Emitters.Add(new UndertaleModLib.UndertaleResourceById<UndertaleModLib.Models.UndertaleParticleSystemEmitter, UndertaleModLib.UndertaleChunkPSEM>());
    }
}