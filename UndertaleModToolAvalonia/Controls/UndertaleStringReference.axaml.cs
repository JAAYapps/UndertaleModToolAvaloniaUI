using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Controls;

public partial class UndertaleStringReference : UserControl
{
    public static readonly StyledProperty<UndertaleString?> ObjectReferenceProperty =
        AvaloniaProperty.Register<UndertaleStringReference, UndertaleString?>(
            nameof(ObjectReference), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsTypeReferenceableProperty =
        AvaloniaProperty.Register<UndertaleStringReference, bool>(nameof(IsTypeReferenceable));

    public static readonly StyledProperty<ICommand?> OpenInTabCommandProperty =
        AvaloniaProperty.Register<UndertaleStringReference, ICommand?>(nameof(OpenInTabCommand));

    public static readonly StyledProperty<ICommand?> OpenInNewTabCommandProperty =
        AvaloniaProperty.Register<UndertaleStringReference, ICommand?>(nameof(OpenInNewTabCommand));

    public static readonly StyledProperty<ICommand?> FindAllReferencesCommandProperty =
        AvaloniaProperty.Register<UndertaleStringReference, ICommand?>(nameof(FindAllReferencesCommand));

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

    public UndertaleString? ObjectReference
    {
        get => GetValue(ObjectReferenceProperty);
        set => SetValue(ObjectReferenceProperty, value);
    }

    public bool IsTypeReferenceable
    {
        get { return GetValue(IsTypeReferenceableProperty); }
        set { SetValue(IsTypeReferenceableProperty, value); }
    }

    public UndertaleStringReference()
    {
        InitializeComponent();
        ObjectText.LostFocus += ObjectText_LostFocus;
        ObjectText.KeyDown += ObjectText_KeyDown;
        ObjectText.AddHandler(DragDrop.DragOverEvent, TextBox_DragOver);
        ObjectText.AddHandler(DragDrop.DropEvent, TextBox_Drop);
        AddHandler(PointerPressedEvent, Details_MouseDown, RoutingStrategies.Tunnel);
        
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

    private bool isCommitting;

    private async void ObjectText_LostFocus(object? sender, RoutedEventArgs e)
    {
        await CommitTextAsync();
    }

    private async void ObjectText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await CommitTextAsync();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            ShowContent(ObjectReference);
        }
    }
    
    private async Task CommitTextAsync()
    {
        if (isCommitting)
            return;

        string text = ObjectText.Text ?? string.Empty;
        UndertaleString? current = ObjectReference;
        if (text == (current?.Content ?? string.Empty))
            return;

        if (AppConstants.Data is null)
        {
            ShowContent(current);
            return;
        }

        if (current is null)
        {
            ObjectReference = AppConstants.Data.Strings.MakeString(text);
            return;
        }

        isCommitting = true;
        try
        {
            int choice = await AskHowToChangeAsync();

            switch (choice)
            {
                case 0:
                    ObjectReference = AppConstants.Data.Strings.MakeString(text);
                    break;
                case 1:
                    current.Content = text;
                    PseudoClasses.Set(":empty-content", text.Length == 0);
                    break;
                default:
                    ShowContent(current);
                    break;
            }
        }
        finally
        {
            isCommitting = false;
        }
    }
    
    private Task<int> AskHowToChangeAsync()
    {
        return App.Current!.ShowChoice(
            "You edited a string reference! What do you want to do?",
            "String reference changed",
            "Change only this one value",
            "Change all occurrences of this value",
            "Abort the change");
    }
    
    private void ShowContent(UndertaleString? value)
    {
        ObjectText.SetCurrentValue(TextBox.TextProperty, value?.Content ?? string.Empty);
    }

    private void TextBox_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = ResourceDragDrop.GetResource(e) is UndertaleString ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void TextBox_Drop(object? sender, DragEventArgs e)
    {
        if (ResourceDragDrop.GetResource(e) is UndertaleString dropped)
        {
            ObjectReference = dropped;
            e.DragEffects = DragDropEffects.Link;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ObjectReferenceProperty)
        {
            var newValue = change.GetNewValue<UndertaleString?>();
            PseudoClasses.Set(":null-reference", newValue is null);
            PseudoClasses.Set(":empty-content", newValue?.Content == string.Empty);
            IsTypeReferenceable = newValue is not null && UndertaleResourceReferenceMap.IsTypeReferenceable(newValue.GetType());
            UpdateContextMenu(newValue);
        }
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateContextMenu(ObjectReference);
    }

    private void UpdateContextMenu(UndertaleString? undertaleString)
    {
        // The TextBox is internally available 
        if (ObjectText == null) return;

        if (undertaleString is not null)
        {
            if (Resources.TryGetValue("menuFlyout", out var menuResource) && menuResource is MenuFlyout menu)
            {
                ObjectText.ContextFlyout = menu;
            }
        }
        else
        {
            ObjectText.ContextFlyout = null;
        }
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
    }
}