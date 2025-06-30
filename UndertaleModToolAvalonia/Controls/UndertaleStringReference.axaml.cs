using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews.FindReferencesTypesDialog;

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
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(FindAllReferencesCommand));

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
        // TODO UndertaleResourceReferenceMap.IsTypeReferenceable(objType);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ObjectReferenceProperty)
        {
            var newValue = change.GetNewValue<UndertaleString?>();
            PseudoClasses.Set(":null-reference", newValue is null);
            PseudoClasses.Set(":empty-content", newValue?.Content == string.Empty);
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
            if (Resources.TryGetValue("contextMenu", out var menuResource) && menuResource is ContextMenu menu)
            {
                menu.DataContext = undertaleString;
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
            var editorVM = App.Current!.Services.GetRequiredService<EditorViewModel>();
            editorVM.OpenAssetInNewTabCommand.Execute(ObjectReference);
        }
    }
}