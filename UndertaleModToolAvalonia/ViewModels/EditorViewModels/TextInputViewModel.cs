using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class TextInputViewModel : ViewModelBase, IInitializable<TextInputParameters>, IDialogViewModel<string>
    {
        [ObservableProperty]
        private string label = "";

        [ObservableProperty]
        private string text = "";

        public string Title { get; private set; } = "";
        public string? Result { get; private set; }
        public void FinalizeResult(bool success)
        {
            Result = success ? Text : null;
        }

        public Task<bool> InitializeAsync(TextInputParameters parameters)
        {
            Title = parameters.Title;
            Label = parameters.Label;
            Text = parameters.DefaultText;
            return Task.FromResult(true);
        }
    }
}
