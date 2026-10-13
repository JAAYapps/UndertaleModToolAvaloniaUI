using Avalonia.Controls;

namespace UndertaleModToolAvalonia.Views.EditorViews;

public partial class TextOutputView : Window
{
    public TextOutputView()
    {
        InitializeComponent();

        CloseButton.Click += (s, e) => Close();
    }
}
