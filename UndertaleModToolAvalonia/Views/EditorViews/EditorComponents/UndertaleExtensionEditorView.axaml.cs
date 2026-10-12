using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using System;
using System.Globalization;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleExtensionOption;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleExtensionEditorView : UserControl
{
    public UndertaleExtensionEditorView()
    {
        InitializeComponent();
    }

    private void KindComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        var option = comboBox?.DataContext as UndertaleExtensionOption;
        if (option?.Value is null)
            return;

        switch (comboBox?.SelectedItem)
        {
            case OptionKind.String:
            case null:
                break;

            case OptionKind.Boolean:
                if (option.Value.Content.ToLowerInvariant() == "true")
                    option.Value.Content = "True";
                else
                    option.Value.Content = "False";
                break;

            case OptionKind.Number:
                if (!Double.TryParse(option.Value.Content, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                    option.Value.Content = "0";
                break;
        }
    }
}

public class OptionValueTemplateSelector : IDataTemplate
{
    public IDataTemplate? StringTemplate { get; set; }
    public IDataTemplate? BooleanTemplate { get; set; }
    public IDataTemplate? NumberTemplate { get; set; }

    public Control? Build(object? param)
    {
        return param switch
        {
            OptionKind.String => StringTemplate?.Build(param),
            OptionKind.Boolean => BooleanTemplate?.Build(param),
            OptionKind.Number => NumberTemplate?.Build(param),
            _ => null
        };
    }

    public bool Match(object? data)
    {
        return data is OptionKind;
    }
}