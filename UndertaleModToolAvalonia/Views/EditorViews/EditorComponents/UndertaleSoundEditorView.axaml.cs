using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SFML.Audio;
using System;
using System.Formats.Tar;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.PlayerService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleSoundEditorView : UserControl
{
    public UndertaleSoundEditorView()
    {
        InitializeComponent();
    }
}