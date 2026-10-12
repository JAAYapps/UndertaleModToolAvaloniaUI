using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleExtensionFunctionEditorView : UserControl
{
    public UndertaleExtensionFunctionEditorView()
    {
        InitializeComponent();
    }

    private void Button_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is not UndertaleExtensionFunctionEditorViewModel vm)
            return;
        vm.AddArgumentCommand?.Execute(null);
    }
}