using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UndertaleModToolAvalonia.Views.EditorViews
{
    public partial class ProfileView : Window
    {
        public ProfileView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
