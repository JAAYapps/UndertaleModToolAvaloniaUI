using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels;

public class PageTemplate
{
    public PageTemplate(Type pageType, string iconKey)
    {
        Label = pageType.Name.Replace("PageViewModel", "");
        PageType = pageType;

        Application.Current!.TryFindResource(iconKey, out var res);
        Icon = (StreamGeometry)res!;
    }
    
    public string Label { get; }
    public Type PageType { get; } 
    public StreamGeometry Icon { get; }
}