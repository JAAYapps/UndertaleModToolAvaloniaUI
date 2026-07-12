using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia;

public class ViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Type> _locator = new();

    public ViewLocator()
    {
        RegisterViews();
    }

    private void RegisterViews()
    {
        // 1. Get the assembly where your Views and ViewModels live
        var assembly = Assembly.GetExecutingAssembly();

        // 2. Find all types that are ViewModels
        var viewModelTypes = assembly.GetExportedTypes()
            .Where(t => t.Name.EndsWith("ViewModel"));

        // 3. Find all types that are Views (UserControls/Windows)
        // Note: Adjust the filter if your views don't end in "View"
        var viewTypes = assembly.GetExportedTypes()
            .Where(t => t.Name.EndsWith("View"))
            .ToDictionary(t => t.Name);

        // 4. Pair them up automatically
        foreach (var vmType in viewModelTypes)
        {
            var viewName = vmType.Name.Replace("ViewModel", "View");
                
            if (viewTypes.TryGetValue(viewName, out var viewType))
            {
                _locator[vmType] = viewType;
            }
        }
    }
    
    public Control? Build(object? data)
    {
        if (data is null) return null;

        if (_locator.TryGetValue(data.GetType(), out var viewType))
        {
            // Create the instance using Activator (Safe because we Rooted the assembly)
            return (Control)Activator.CreateInstance(viewType)!;
        }

        return new TextBlock { Text = "Not Found: " + data.GetType().Name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
    
    /*public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }*/
}