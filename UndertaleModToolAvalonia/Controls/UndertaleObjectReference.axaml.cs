using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Controls;

public partial class UndertaleObjectReference : UserControl
{
    private static readonly Regex camelCaseRegex = new("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
    private static readonly char[] vowels = { 'a', 'o', 'u', 'e', 'i', 'y' };

    public static readonly StyledProperty<object?> ObjectReferenceProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, object?>(
            nameof(ObjectReference), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<Type?> ObjectTypeProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, Type?>(
        nameof(ObjectType), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool?> CanRemoveProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, bool?>(
        nameof(CanRemove), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool?> CanChangeProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, bool?>(
        nameof(CanChange), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleGameObject?> GameObjectProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, UndertaleGameObject?>(
        nameof(GameObject), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<EventType?> ObjectEventTypeProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, EventType?>(
        nameof(ObjectEventType), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<uint?> ObjectEventSubtypeProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, uint?>(
        nameof(ObjectEventSubtype), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleRoom?> RoomProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, UndertaleRoom?>(
        nameof(Room), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleRoom.GameObject?> RoomGameObjectProperty =
    AvaloniaProperty.Register<UndertaleObjectReference, UndertaleRoom.GameObject?>(
        nameof(RoomGameObject), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsTypeReferenceableProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, bool>(nameof(IsTypeReferenceable));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
       AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(RemoveCommand));

    public static readonly StyledProperty<ICommand?> OpenInTabCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(OpenInTabCommand));

    public static readonly StyledProperty<ICommand?> OpenInNewTabCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(OpenInNewTabCommand));

    public static readonly StyledProperty<ICommand?> FindAllReferencesCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(FindAllReferencesCommand));

    public ICommand? RemoveCommand
    { 
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    public ICommand? OpenInTabCommand
    { 
        get => GetValue(OpenInTabCommandProperty);
        set => SetValue(OpenInTabCommandProperty, value);
    }

    public ICommand? OpenInNewTabCommand
    {
        get => GetValue(OpenInNewTabCommandProperty);
        set => SetValue(OpenInNewTabCommandProperty, value);
    }

    public ICommand? FindAllReferencesCommand
    {
        get => GetValue(FindAllReferencesCommandProperty);
        set => SetValue(FindAllReferencesCommandProperty, value);
    }

    public object ObjectReference
    {
        get { return GetValue(ObjectReferenceProperty)!; }
        set { SetValue(ObjectReferenceProperty, value); }
    }

    public Type ObjectType
    {
        get { return GetValue(ObjectTypeProperty)!; }
        set { SetValue(ObjectTypeProperty, value); }
    }

    public bool CanRemove
    {
        get { return (bool)GetValue(CanRemoveProperty)!; }
        set { SetValue(CanRemoveProperty, value); }
    }

    public bool CanChange
    {
        get { return (bool)GetValue(CanChangeProperty); }
        set { SetValue(CanChangeProperty, value); }
    }

    public bool IsTypeReferenceable
    {
        get { return (bool)GetValue(IsTypeReferenceableProperty); }
        set { SetValue(IsTypeReferenceableProperty, value); }
    }

    public UndertaleGameObject GameObject
    {
        get { return (UndertaleGameObject)GetValue(GameObjectProperty); }
        set { SetValue(GameObjectProperty, value); }
    }

    public EventType ObjectEventType
    {
        get { return (EventType)GetValue(ObjectEventTypeProperty); }
        set { SetValue(ObjectEventTypeProperty, value); }
    }

    public uint ObjectEventSubtype
    {
        get { return (uint)GetValue(ObjectEventSubtypeProperty); }
        set { SetValue(ObjectEventSubtypeProperty, value); }
    }

    public UndertaleRoom Room
    {
        get { return (UndertaleRoom)GetValue(RoomProperty); }
        set { SetValue(RoomProperty, value); }
    }

    public UndertaleRoom.GameObject RoomGameObject
    {
        get { return (UndertaleRoom.GameObject)GetValue(RoomGameObjectProperty); }
        set { SetValue(RoomGameObjectProperty, value); }
    }

    public bool IsPreCreate { get; set; } = false;

    public UndertaleObjectReference()
    {
        InitializeComponent();
        // TODO put UndertaleResourceReferenceMap.IsTypeReferenceable(objType); somewhere.
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ObjectTypeProperty)
        {
            var newType = change.GetNewValue<Type?>();

            Classes.Set("is-code", newType == typeof(UndertaleCode));
        }
        else if (change.Property == ObjectReferenceProperty)
        {
            UpdateContextMenu(change.GetNewValue<object?>());
        }
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateContextMenu(ObjectReference);
    }

    private void UpdateContextMenu(object? undertaleObject)
    {
        // The TextBox is internally available 
        if (ObjectText == null) return;

        if (undertaleObject is not null)
        {
            if (this.Resources.TryGetValue("contextMenu", out var menuResource) && menuResource is ContextMenu menu)
            {
                menu.DataContext = undertaleObject;
                ObjectText.ContextMenu = menu;
            }
        }
        else
        {
            ObjectText.ContextMenu = null;
        }
    }

    private void Details_MouseDown(object sender, PointerPressedEventArgs e)
    {
        if (ObjectReference is null)
            return;
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsMiddleButtonPressed)
        {
            if (OpenInNewTabCommand?.CanExecute(ObjectReference) == true)
            {
                OpenInNewTabCommand.Execute(ObjectReference);
            }
        }
    }

    /*private void TextBox_DragOver(object sender, DragEventArgs e)
    {
        UndertaleString sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleString;

        e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void TextBox_Drop(object sender, DragEventArgs e)
    {
        UndertaleString sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleString;

        e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null ? DragDropEffects.Link : DragDropEffects.None;
        if (e.Effects == DragDropEffects.Link)
        {
            ObjectReference = sourceItem;
        }
        e.Handled = true;
    }*/

    /*private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = sender as TextBox;
        var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
        if (binding.IsDirty)
        {
            if (ObjectReference != null)
            {
                StringUpdateWindow dialog = new StringUpdateWindow();
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                switch (dialog.Result)
                {
                    case StringUpdateWindow.ResultType.ChangeOneValue:
                        ObjectReference = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString(tb.Text);
                        break;
                    case StringUpdateWindow.ResultType.ChangeReferencedValue:
                        binding.UpdateSource();
                        break;
                    case StringUpdateWindow.ResultType.Cancel:
                        binding.UpdateTarget();
                        break;
                }
            }
            else
            {
                ObjectReference = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString(tb.Text);
            }
        }
    }*/
}