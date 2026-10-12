using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.ClipboardService
{
    public class ClipboardService : IClipboardService
    {
        public async Task SetTextAsync(string text)
        {
            IClipboard? clipboard = GetTopLevel()?.Clipboard;
            if (clipboard is null)
                throw new InvalidOperationException("The clipboard is not available");

            await clipboard.SetTextAsync(text);
        }

        private static TopLevel? GetTopLevel()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.Windows.FirstOrDefault(x => x.IsActive) ?? desktop.MainWindow;
            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                return TopLevel.GetTopLevel(singleView.MainView);

            return null;
        }
    }
}
