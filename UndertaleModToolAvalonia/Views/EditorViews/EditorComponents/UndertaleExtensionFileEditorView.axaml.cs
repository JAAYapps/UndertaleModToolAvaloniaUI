using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleExtensionFileEditorView : UserControl
{
    public UndertaleExtensionFileEditorView()
    {
        InitializeComponent();
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleExtensionFileEditorViewModel vm)
            return;
        vm.AddFunctionCommand?.Execute(null);
    }
}