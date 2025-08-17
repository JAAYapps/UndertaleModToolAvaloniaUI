using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using UndertaleModToolAvalonia.Utilities;

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

            var window = this.GetVisualRoot() as Window;
            if (window != null)
            {
                TopMenu.DataContext = window.DataContext;
            }
        }
    }
}
