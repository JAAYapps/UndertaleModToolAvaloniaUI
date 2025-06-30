using Avalonia;
using Avalonia.Controls;

namespace UndertaleModToolAvalonia.Controls
{
    public class DataUserControl : UserControl
    {
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            // prevent Avalonia binding errors (and unnecessary "DataContextChanged" firing) when switching to incompatible data type
            if (e.NewValue is null && e.Property == DataContextProperty)
                return;

            base.OnPropertyChanged(e);
        }
    }
}
