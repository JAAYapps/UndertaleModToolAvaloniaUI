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

        [ObservableProperty]
        private bool isMultiline;

        public string Title { get; private set; } = "";
        public string PrimaryButtonText { get; private set; } = "OK";
        public string CloseButtonText { get; private set; } = "Cancel";
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
            IsMultiline = parameters.IsMultiline;
            PrimaryButtonText = parameters.SubmitText;
            CloseButtonText = parameters.CancelText;
            return Task.FromResult(true);
        }
    }
}
