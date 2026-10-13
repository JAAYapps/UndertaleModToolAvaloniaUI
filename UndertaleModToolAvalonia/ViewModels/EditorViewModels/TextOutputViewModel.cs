using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class TextOutputViewModel : ViewModelBase, IInitializable<TextOutputParameters>
    {
        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string label = "";

        [ObservableProperty]
        private string text = "";

        [ObservableProperty]
        private bool isMultiline;

        [ObservableProperty]
        private bool isReadOnly;

        public Task<bool> InitializeAsync(TextOutputParameters parameters)
        {
            Title = parameters.Title;
            Label = parameters.Label;
            Text = parameters.Text;
            IsMultiline = parameters.IsMultiline;
            IsReadOnly = parameters.IsReadOnly;
            return Task.FromResult(true);
        }
    }
}
