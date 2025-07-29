using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleGeneralInfoEditorView : UserControl
{
    public UndertaleGeneralInfoEditorView()
    {
        InitializeComponent();
        DebuggerCheckBox.PointerPressed += DebuggerCheckBox_PreviewMouseLeftButtonDown;
        RoomSyncButton.Click += SyncRoomList_Click;
    }

    private async void DebuggerCheckBox_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not CheckBox checkBox)
            return;

        if (checkBox.IsChecked != true)
            return;

        e.Handled = true;
        var result = await App.Current!.ShowQuestion("Are you sure that you want to enable GMS debugger?\n" +
                                             "If you want to enable a debug mode in some game, then you need to use one of the scripts.");
        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
            checkBox.IsChecked = false;
    }

    private void SyncRoomList_Click(object? sender, RoutedEventArgs e)
    {
        IList<UndertaleRoom> rooms = AppConstants.Data.Rooms;
        IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = (DataContext as UndertaleGeneralInfoEditorViewModel).Info.RoomOrder;
        roomOrder.Clear();
        foreach (var room in rooms)
            roomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room });
    }
}