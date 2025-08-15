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
            //Console.WriteLine("Got focus: " + e.Source);
            if (e.Source is TextBox || e.Source is MenuItem || e.Source is DataGrid)
            {
                blockLostFocus = true;
                ((Control)e.Source).Focus();
            }
        }

        private void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            //Console.WriteLine("Lost focus: " + e.Source);
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
