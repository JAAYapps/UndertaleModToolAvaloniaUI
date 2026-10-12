using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.ClipboardService
{
    public interface IClipboardService
    {
        public Task SetTextAsync(string text);
    }
}
