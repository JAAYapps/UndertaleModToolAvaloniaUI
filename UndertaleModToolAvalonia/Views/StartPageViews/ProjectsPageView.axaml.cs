using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

            this.Loaded += OnLoaded;
            this.LostFocus += OnLostFocus;
            this.GotFocus += OnGotFocus;
        }

        private void OnGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (e.Source is TextBox box)
            {
                blockLostFocus = true;
                box.Focus();
            }
        }

        private void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            if (blockLostFocus)
            {
                blockLostFocus = false;
                return;
            }
            this.Focus();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            this.Focus();
        }
    }
}
