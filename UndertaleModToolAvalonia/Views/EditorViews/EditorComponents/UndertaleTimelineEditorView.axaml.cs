using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleTimelineEditorView : UserControl
{
    public UndertaleTimelineEditorView()
    {
        InitializeComponent();
    }

    private void Button_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not UndertaleTimelineEditorViewModel vm)
            return;

        UndertaleTimeline.UndertaleTimelineMoment obj = new UndertaleTimeline.UndertaleTimelineMoment();

        // find the last timeline moment (which should have the biggest step value)
        var lastMoment = vm.UndertaleTimeline.Moments.LastOrDefault();

        // the default value is 0 anyway.
        if (lastMoment != null)
            obj.Step = lastMoment.Step + 1;

        // make an empty event with a null code entry.
        obj.Event = new UndertalePointerList<UndertaleGameObject.EventAction>();
        obj.Event.Add(new UndertaleGameObject.EventAction());

        // we're done here.
        vm.UndertaleTimeline.Moments.Add(obj);
        UpdateLayout();
    }
}