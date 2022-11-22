using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UndertaleModToolAvalonia.Views.EditorViews
{
    public partial class EditorView : Window
    {
        public EditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
