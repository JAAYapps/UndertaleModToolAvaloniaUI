using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UndertaleModToolAvalonia.Views.MainWindowViews
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
