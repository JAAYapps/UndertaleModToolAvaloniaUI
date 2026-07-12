using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Views.EditorViews
{
    public partial class RuntimePickerView : ContentDialog
    {
        public RuntimePickerView()
        {
            InitializeComponent();
            if (DataContext is RuntimePickerViewModel viewModel)
            {
                if (viewModel.Selected == null && viewModel.Runtimes.Count == 0)
                    this.CloseButtonCommand.Execute(null);
            }
        }

        private void Button_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is RuntimePickerViewModel viewModel)
            {
                var selectedRuntime = viewModel.Selected;
                this.CloseButtonCommand.Execute(selectedRuntime);
            }
            this.CloseButtonCommand.Execute(null);
        }
    }
}