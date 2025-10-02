using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using UndertaleModToolAvalonia;
using Microsoft.Extensions.Configuration;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        App.LoadServices();
        return BuildAvaloniaApp()
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
