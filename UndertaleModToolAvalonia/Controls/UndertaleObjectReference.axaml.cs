using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Controls;

public partial class UndertaleObjectReference : UserControl
{
    private static readonly Regex camelCaseRegex = new("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
    private static readonly char[] vowels = { 'a', 'o', 'u', 'e', 'i', 'y' };

    public static readonly StyledProperty<object> ObjectReferenceProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, object>(
            nameof(ObjectReference), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<Type?> ObjectTypeProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, Type?>(
            nameof(ObjectType), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleData?> UndertaleDataProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, UndertaleData?>(
            nameof(UndertaleData), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool?> CanRemoveProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, bool?>(
            nameof(CanRemove), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CanChangeProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, bool>(
            nameof(CanChange), defaultValue: true, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleGameObject> GameObjectProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, UndertaleGameObject>(
            nameof(GameObject), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<EventType> ObjectEventTypeProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, EventType>(
            nameof(ObjectEventType), defaultValue: EventType.Other, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<uint> ObjectEventSubtypeProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, uint>(
            nameof(ObjectEventSubtype), defaultValue: 0, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleRoom> RoomProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, UndertaleRoom>(
            nameof(Room), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleRoom.GameObject> RoomGameObjectProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, UndertaleRoom.GameObject>(
            nameof(RoomGameObject), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsTypeReferenceableProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, bool>(nameof(IsTypeReferenceable));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(RemoveCommand));

    public static readonly StyledProperty<object?> RemoveCommandParameterProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, object?>(nameof(RemoveCommandParameter));   

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

    public object? RemoveCommandParameter
    {
        get => GetValue(RemoveCommandParameterProperty);
        set => SetValue(RemoveCommandParameterProperty, value);
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
        get => GetValue(ObjectReferenceProperty)!;
        set => SetValue(ObjectReferenceProperty, value);
    }

    public Type ObjectType
    {
        get => GetValue(ObjectTypeProperty)!;
        set => SetValue(ObjectTypeProperty, value);
    }

    public UndertaleData UndertaleData
    {
        get => GetValue(UndertaleDataProperty)!;
        set => SetValue(UndertaleDataProperty, value);
    }

    public bool CanRemove
    {
        get { return (bool)GetValue(CanRemoveProperty)!; }
        set => SetValue(CanRemoveProperty, value);
    }

    public bool CanChange
    {
        get => GetValue(CanChangeProperty);
        set => SetValue(CanChangeProperty, value);
    }

    public bool IsTypeReferenceable
    {
        get => GetValue(IsTypeReferenceableProperty);
        set => SetValue(IsTypeReferenceableProperty, value);
    }

    public UndertaleGameObject GameObject
    {
        get => GetValue(GameObjectProperty);
        set => SetValue(GameObjectProperty, value);
    }

    public EventType ObjectEventType
    {
        get => GetValue(ObjectEventTypeProperty);
        set => SetValue(ObjectEventTypeProperty, value);
    }

    public uint ObjectEventSubtype
    {
        get => (uint)GetValue(ObjectEventSubtypeProperty);
        set => SetValue(ObjectEventSubtypeProperty, value);
    }

    public UndertaleRoom Room
    {
        get => GetValue(RoomProperty);
        set => SetValue(RoomProperty, value);
    }

    public UndertaleRoom.GameObject RoomGameObject
    {
        get => GetValue(RoomGameObjectProperty);
        set => SetValue(RoomGameObjectProperty, value);
    }

    public bool IsPreCreate { get; set; } = false;

    public UndertaleObjectReference()
    {
        InitializeComponent();
        Loaded += UndertaleObjectReference_Loaded;
        DetailsButton.Click += DetailsButton_Click;
        ObjectText.AddHandler(DragDrop.DragOverEvent, TextBox_DragOver);
        ObjectText.AddHandler(DragDrop.DropEvent, TextBox_Drop);
        AddHandler(PointerPressedEvent, Details_MouseDown, RoutingStrategies.Tunnel);
        RemoveCommand = new RelayCommand(() => ObjectReference = null);
        
        OpenInTabCommand = new RelayCommand<object>(asset => SendOpen(asset, inNewTab: false));
        OpenInNewTabCommand = new RelayCommand<object>(asset => SendOpen(asset, inNewTab: true));
        FindAllReferencesCommand = new RelayCommand<object>(asset =>
        {
            if (asset is not null)
                WeakReferenceMessenger.Default.Send(new FindReferencesMessage(asset));
        });
    }

    private static void SendOpen(object? asset, bool inNewTab)
    {
        if (asset is not null)
            WeakReferenceMessenger.Default.Send(new OpenAssetMessage(asset, inNewTab));
    }

    private void UndertaleObjectReference_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ObjectType is null)
            return;

        if (!this.TryFindResource("NullLabel", out var resource) || resource is not TextBlock label)
            return;

        string typeName = ObjectType.ToString();
        string n = "";
        if (typeName.StartsWith("UndertaleModLib.Models.Undertale"))
        {
            // "UndertaleAudioGroup" -> "audio group"
            typeName = typeName["UndertaleModLib.Models.Undertale".Length..];
            typeName = camelCaseRegex.Replace(typeName, " $1").ToLowerInvariant();
        }
        // If the first letter is a vowel
        if (Array.IndexOf(vowels, typeName[0]) != -1)
            n = "n";

        if (CanChange)
            label.Text = $"(drag & drop a{n} {typeName})";
        else
            label.Text = $"(empty {typeName} reference)";
        label.UpdateLayout();
        UpdatePseudoClass(ObjectReference);
    }

    private void UpdatePseudoClass(object? value)
    {
        PseudoClasses.Set(":null-reference", value is null);
    }

    private void DetailsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ObjectReference is null)
        {
            if (GameObject is not null)
            {
                ObjectReference = GameObject.EventHandlerFor(ObjectEventType, ObjectEventSubtype, UndertaleData);
            }
            else if (Room is not null)
            {
                if (RoomGameObject is null)
                {
                    // Generate base name
                    string name = $"gml_Room_{Room.Name.Content}_Create";

                    // If code already exists, use it (otherwise create new code)
                    if (UndertaleData.Code.ByName(name) is UndertaleCode existing)
                    {
                        _ = App.Current!.ShowWarning("Code entry for room already exists; reusing it.");
                        ObjectReference = existing;
                    }
                    else
                    {
                        ObjectReference = UndertaleCode.CreateEmptyEntry(UndertaleData, name);
                    }
                }
                else
                {
                    // Generate base name
                    string beginning = $"gml_RoomCC_{Room.Name.Content}_{RoomGameObject.InstanceID}";
                    string suffix = !IsPreCreate ? "_Create" : "_PreCreate";
                    string name = beginning + suffix;

                    // Ensure no duplicate names (in case instance IDs change)
                    int i = 0;
                    while (UndertaleData.Code.ByName(name) is not null)
                    {
                        name = beginning + "_" + (i++).ToString() + suffix;
                    }

                    ObjectReference = UndertaleCode.CreateEmptyEntry(UndertaleData, name);
                }
            }
            else
            {
                _ = App.Current!.ShowError("Adding not supported in this situation.");
            }
        }
        else
        {
            if (OpenInTabCommand?.CanExecute(ObjectReference) == true)
                OpenInTabCommand.Execute(ObjectReference);
        }
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
            object? newValue = change.GetNewValue<object?>();
            UpdatePseudoClass(newValue);
            IsTypeReferenceable = newValue is not null && UndertaleResourceReferenceMap.IsTypeReferenceable(newValue.GetType());

            UpdateContextMenu(newValue);
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

        if (undertaleObject is not null && this.Resources.TryGetValue("contextFlyout", out var menuResource) && menuResource is MenuFlyout menu)
            ObjectText.ContextFlyout = menu;
        else
            ObjectText.ContextFlyout = null;
    }

    private void Details_MouseDown(object? sender, PointerPressedEventArgs e)
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
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed && e.ClickCount == 2
                 && e.Source is Visual source && (source == ObjectText || ObjectText.IsVisualAncestorOf(source)))
        {
            if (OpenInTabCommand?.CanExecute(ObjectReference) == true)
            {
                OpenInTabCommand.Execute(ObjectReference);
            }
            e.Handled = true;
        }
    }
    
    private UndertaleObject? AcceptedResource(DragEventArgs e)
    {
        if (!CanChange || ObjectType is null)
            return null;

        UndertaleObject? resource = ResourceDragDrop.GetResource(e);
        return resource is not null && ObjectType.IsInstanceOfType(resource) ? resource : null;
    }

    // Drop works but cursor stays red on Linux (Not sure if it's just Solus OS in Wayland). Avalonia issue, not this code.
    private void TextBox_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = AcceptedResource(e) is null ? DragDropEffects.None : DragDropEffects.Link;
        e.Handled = true;
    }

    private void TextBox_Drop(object? sender, DragEventArgs e)
    {
        if (AcceptedResource(e) is UndertaleObject resource)
        {
            ObjectReference = resource;
            e.DragEffects = DragDropEffects.Link;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }
}