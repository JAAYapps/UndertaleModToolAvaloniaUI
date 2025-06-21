using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using System.IO;
using System.Runtime.InteropServices;
using LibMpv.Client;
using log4net;

namespace UndertaleModToolAvalonia
{
    class Program
    {
        public static bool completeFail = false;
        public static string failMessage = string.Empty;

        public static string GetExecutableDirectory() => Path.GetDirectoryName(Environment.ProcessPath)!;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            // Figure out cross platform file association for registering the mod tool in Windows, Mac, and Linux as the program to open data.wim files for Undertale.
            
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
                AppDomain currentDomain = AppDomain.CurrentDomain;
                // Handler for unhandled exceptions.
                currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
                // Handler for exceptions in threads behind forms.
                var builder = BuildAvaloniaApp();
                // InitMpv();
                return builder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                completeFail = true;
                failMessage = e.Message + "\r\n" + e.StackTrace;
                Console.WriteLine(failMessage);
                File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash.txt"), e.ToString());
                var builder = BuildAvaloniaApp();
                builder.StartWithClassicDesktopLifetime(args);
            }

            return 0;
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            ILog log = LogManager.GetLogger(typeof(Program));
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash2.txt"), (ex.ToString() + "\n" + ex.Message + "\n" + ex.StackTrace));
        }

        private static void GlobalThreadExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            ILog log = LogManager.GetLogger(typeof(Program)); //Log4NET
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash3.txt"), (ex.Message + "\n" + ex.StackTrace));
        }
        
        public static void InitMpv()
        {
            var platform = IntPtr.Size == 8 ? "x86_64" : "x86";
            var platformId = FunctionResolverFactory.GetPlatformId();
            Console.WriteLine("Check system.");
            if (platformId == LibMpvPlatformID.Win32NT)
            {
                Console.WriteLine("Windows is the system.");
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, platform);
                LibMpv.Client.LibMpv.UseLibMpv(2).UseLibraryPath(path);
            }
            else if (platformId == LibMpvPlatformID.Unix)
            {
                Console.WriteLine("Unix is the system.");
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    Console.WriteLine("arm64 is the system.");
                    var path = $"/usr/lib/aarch64-linux-gnu/";
                    LibMpv.Client.LibMpv.UseLibMpv(2).UseLibraryPath(path);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    Console.WriteLine("x64 is the system.");
                    // var path = $"/usr/lib/{platform}-linux-gnu";
                    var path = $"/usr/lib64/";
                    LibMpv.Client.LibMpv.UseLibMpv(2).UseLibraryPath(path);
                }
            }
        }
        
        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
