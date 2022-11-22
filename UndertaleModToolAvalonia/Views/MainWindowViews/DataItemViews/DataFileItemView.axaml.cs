using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UndertaleModToolAvalonia.Views.MainWindowViews.DataItemViews
{
    public partial class DataFileItemView : UserControl
    {
        public DataFileItemView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
