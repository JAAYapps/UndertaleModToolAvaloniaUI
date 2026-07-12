using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

namespace UndertaleModToolAvalonia.Browser;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        await JSHost.ImportAsync("WebAudioPlayer", "../WebAudioPlayer.js");
        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var appBuilder = AppBuilder.Configure<App>();
        // var assembly = Assembly.GetExecutingAssembly();
        // using var stream = assembly.GetManifestResourceStream("Silicon Testimonies.Browser.appsettings.json");
        // if (stream != null)
        // {
        //     var config = new ConfigurationBuilder()
        //         .AddJsonStream(stream)
        //         .Build();
        //     appBuilder.With(config);
        // }
        return appBuilder;//.UseBrowser();
    }
}