using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using System.Threading.Tasks;
using Splat;
using log4net;
using System.IO;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia
{
    class Program
    {
        public static string GetExecutableDirectory()
        {
            return Path.GetDirectoryName(Environment.ProcessPath);
        }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Contains("--wait-for-attach"))
            {
                Console.WriteLine("Attach debugger and use 'Set next statement'");
                while (true)
                {
                    Thread.Sleep(100);
                    if (Debugger.IsAttached)
                        break;
                }
            }

            try
            {
                AppDomain currentDomain = default(AppDomain);
                currentDomain = AppDomain.CurrentDomain;
                // Handler for unhandled exceptions.
                currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash.txt"), e.ToString());
                MessageBox.Show(e.ToString());
            }
            var builder = BuildAvaloniaApp();

            return builder.StartWithClassicDesktopLifetime(args);
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;
            ILog log = LogManager.GetLogger(typeof(Program));
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash2.txt"), (ex.ToString() + "\n" + ex.Message + "\n" + ex.StackTrace));
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions
                {
                    EnableMultiTouch = true,
                    UseDBusMenu = true,
                    EnableIme = true,
                    UseGpu = true,
                    UseEGL = true
                })
                .With(new Win32PlatformOptions { AllowEglInitialization = true })
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                .UseSkia()
                .LogToTrace()
                .UseReactiveUI()
                .UseManagedSystemDialogs();
    }
}
