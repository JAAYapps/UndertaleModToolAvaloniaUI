using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Runtime.Serialization;
using System.Windows.Input;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Controls;

public partial class AudioFileReference : UserControl
{
    public static readonly StyledProperty<UndertaleEmbeddedAudio?> AudioReferenceProperty =
        AvaloniaProperty.Register<AudioFileReference, UndertaleEmbeddedAudio?>(
        nameof(AudioReference), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UndertaleAudioGroup?> GroupReferenceProperty =
        AvaloniaProperty.Register<AudioFileReference, UndertaleAudioGroup?>(
        nameof(GroupReference), defaultValue: null, inherits: false, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int?> AudioIDProperty =
        AvaloniaProperty.Register<AudioFileReference, int?>(nameof(AudioID));

    public static readonly StyledProperty<int?> GroupIDProperty =
        AvaloniaProperty.Register<AudioFileReference, int?>(nameof(GroupID));

    public static readonly StyledProperty<bool> IsTypeReferenceableProperty =
        AvaloniaProperty.Register<UndertaleStringReference, bool>(nameof(IsTypeReferenceable));

    public static readonly StyledProperty<ICommand?> OpenInTabCommandProperty =
        AvaloniaProperty.Register<UndertaleStringReference, ICommand?>(nameof(OpenInTabCommand));

    public static readonly StyledProperty<ICommand?> OpenInNewTabCommandProperty =
        AvaloniaProperty.Register<UndertaleStringReference, ICommand?>(nameof(OpenInNewTabCommand));

    public static readonly StyledProperty<ICommand?> FindAllReferencesCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(FindAllReferencesCommand));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<UndertaleObjectReference, ICommand?>(nameof(RemoveCommand));

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

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    public UndertaleEmbeddedAudio AudioReference
    {
        get => GetValue(AudioReferenceProperty);
        set { SetValue(AudioReferenceProperty, value); }
    }

    public UndertaleAudioGroup GroupReference
    {
        get => GetValue(GroupReferenceProperty);
        set { SetValue(GroupReferenceProperty, value); }
    }

    public bool IsTypeReferenceable
    {
        get { return GetValue(IsTypeReferenceableProperty); }
        set { SetValue(IsTypeReferenceableProperty, value); }
    }

    public int AudioID
    {
        get => (int)GetValue(AudioIDProperty);
        set { SetValue(AudioIDProperty, value); }
    }

    public int GroupID
    {
        get => (int)GetValue(GroupIDProperty);
        set { SetValue(GroupIDProperty, value); }
    }

    public AudioFileReference()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AudioReferenceProperty)
        {
            var newValue = change.GetNewValue<UndertaleEmbeddedAudio?>();
            UpdateContextMenu(newValue);
        }
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateContextMenu(AudioReference);
    }

    private void UpdateContextMenu(UndertaleEmbeddedAudio? undertaleEmbeddedAudio)
    {
        // The TextBox is internally available 
        if (ObjectText == null) return;

        if (undertaleEmbeddedAudio is not null)
        {
            if (Resources.TryGetValue("contextMenu", out var menuResource) && menuResource is ContextMenu menu)
            {
                menu.DataContext = undertaleEmbeddedAudio;
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
        if (AudioReference is null)
            return;
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsMiddleButtonPressed)
        {
            if (OpenInNewTabCommand?.CanExecute(AudioReference) == true)
            {
                OpenInNewTabCommand.Execute(AudioReference);
            }
        }
    }
}