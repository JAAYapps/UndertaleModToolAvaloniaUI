using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.IO;
using System.Linq;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.Views.StartPageViews
{
    public partial class ProjectsPageView : UserControl
    {
        private bool blockLostFocus = false;

        public ProjectsPageView()
        {
            InitializeComponent();
            this.Loaded += ProjectsPageView_Loaded;
        }

        private void ProjectsPageView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (TopMenu == null) return;

            if (TopLevel.GetTopLevel(this) is Window window)
            {
                TopMenu.DataContext = window.DataContext;
            }
        }

        private void RootMenuItem_SubmenuOpened(object? sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, Path.Join(Settings.GetExecutableDirectory(), "Scripts"));
        }

        private void MenuItem_RunScript_SubmenuOpened(object? sender, RoutedEventArgs e, string folderDir)
        {
            if (sender is not MenuItem item || e.Source != item)
                return;
            if (TopMenu.DataContext is not MainWindowViewModel vm)
                return;

            e.Handled = true;

            DirectoryInfo directory = new DirectoryInfo(folderDir);
            item.Items.Clear();
            try
            {
                if (!directory.Exists)
                {
                    item.Items.Add(new MenuItem { Header = $"(Path {folderDir} does not exist, cannot search for files!)", IsEnabled = false });
                }
                else
                {
                    foreach (var file in directory.EnumerateFiles("*.csx").OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        item.Items.Add(new MenuItem
                        {
                            Header = file.Name.Replace("_", "__"),
                            Command = vm.RunBuiltinScriptCommand,
                            CommandParameter = file.FullName
                        });
                    }

                    foreach (var subDirectory in directory.EnumerateDirectories().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!subDirectory.EnumerateFiles("*.csx").Any())
                            continue;

                        MenuItem subItem = new() { Header = subDirectory.Name.Replace("_", "__") };
                        subItem.Items.Add(new MenuItem { Header = "(loading...)", IsEnabled = false });
                        subItem.SubmenuOpened += (o, args) => MenuItem_RunScript_SubmenuOpened(o, args, subDirectory.FullName);
                        item.Items.Add(subItem);
                    }

                    if (item.Items.Count == 0)
                        item.Items.Add(new MenuItem { Header = "(No scripts found!)", IsEnabled = false });
                }
            }
            catch (Exception err)
            {
                item.Items.Add(new MenuItem { Header = err.ToString(), IsEnabled = false });
            }

            if (item.Name != "RootScriptItem") return;

            item.Items.Add(new MenuItem
            {
                Header = "Run _other script...",
                Command = vm.RunOtherScriptCommand,
                CommandParameter = this
            });
        }
    }
}
