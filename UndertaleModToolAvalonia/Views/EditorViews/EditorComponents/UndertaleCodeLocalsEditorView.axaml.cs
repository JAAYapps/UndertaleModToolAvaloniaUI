using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleCodeLocalsEditorView : UserControl
{
    public UndertaleCodeLocalsEditorView()
    {
        InitializeComponent();
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleCodeLocalsEditorViewModel vm)
            return;
        vm.AddLocalCommand?.Execute(null);
    }
}