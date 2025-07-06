using Avalonia.Controls;
using Avalonia.Interactivity;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Views.EditorViews
{
    public partial class RuntimePickerView : Window
    {
        public RuntimePickerView()
        {
            InitializeComponent();
            if (DataContext is RuntimePickerViewModel viewModel)
            {
                if (viewModel.Selected == null && viewModel.Runtimes.Count == 0)
                    Close();
            }
        }

        private void Button_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is RuntimePickerViewModel viewModel)
            {
                var selectedRuntime = viewModel.Selected;
                Close(selectedRuntime);
            }
            Close();
        }
    }
}