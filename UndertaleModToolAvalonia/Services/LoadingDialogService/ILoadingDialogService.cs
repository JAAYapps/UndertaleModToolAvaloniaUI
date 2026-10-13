using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.LoadingDialogService;

public interface ILoadingDialogService
{
    void Initialize();
    void Show(string title = "Loading...", string message = "Please wait...");
    void Hide();
    Task UpdateMessageAsync(string message);
    Task UpdateStatusAsync(string status);
    Task UpdateProgressAsync(double value, double max);
    Task SetIndeterminateAsync(bool isIndeterminate);
}