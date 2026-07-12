using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Dialogs;
using log4net;

namespace UndertaleModToolAvalonia.Desktop
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            Console.WriteLine("Started.");
            // Figure out cross platform file association for registering the mod tool in Windows, Mac, and Linux as the program to open data.wim files for Undertale.
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                try 
                {
                    // Fix HarfBuzz (Text Rendering)
                    // Tricking the app into using the system C library instead of the missing "Sharp" wrapper
                    var harfbuzzAssembly = Assembly.Load("HarfBuzzSharp");
                    NativeLibrary.SetDllImportResolver(harfbuzzAssembly, (libraryName, assembly, searchPath) => 
                    {
                        if (libraryName == "libHarfBuzzSharp") 
                        {
                            return NativeLibrary.Load("/usr/local/lib/libharfbuzz.so.0");
                        }
                        return IntPtr.Zero;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FreeBSD Patch Error]: {ex.Message}");
                }
            }
            
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
                if (args.Contains("--drm"))
                {
                    SilenceConsole();

                    var pythonVenvLibPath = Environment.CurrentDirectory + "/.venv/lib/python3.11/site-packages/llvmlite/binding/";

                    // Get the current LD_LIBRARY_PATH and prepend the new path
                    var currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
                    Console.WriteLine("LD_LIBRARY_PATH: " + currentLdPath);
                    var newLdPath = pythonVenvLibPath + ":" + currentLdPath;
                    Console.WriteLine("New LD_LIBRARY_PATH: " + newLdPath);
                    // Set it for this process
                    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", newLdPath);

                    string? card = null;
                    card = args.FirstOrDefault((str) => str.StartsWith("/dev/dri/"));
                    Console.WriteLine("Card: " + card);
                    // By default, Avalonia will try to detect output card automatically.
                    // But you can specify one, for example "/dev/dri/card1".
                    return builder.StartLinuxDrm(args: args, card: card, scaling: 1.0);
                }
                return builder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                Settings.completeFail = true;
                Settings.failMessage = e.Message + "\r\n" + e.StackTrace;
                Console.WriteLine(Settings.failMessage);
                File.WriteAllText(Path.Combine(Settings.GetExecutableDirectory() ?? string.Empty, "crash.txt"), e.ToString());
            }

            return 0;
        }

        private static void SilenceConsole()
        {
            new Thread(() =>
                {
                    Console.CursorVisible = false;
                    while (true)
                        Console.ReadKey(true);
                })
                { IsBackground = true }.Start();
        }
        
        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            ILog log = LogManager.GetLogger(typeof(Program));
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(Settings.GetExecutableDirectory() ?? string.Empty, "crash2.txt"), (ex.ToString() + "\n" + ex.Message + "\n" + ex.StackTrace));
        }

        private static void GlobalThreadExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            ILog log = LogManager.GetLogger(typeof(Program)); //Log4NET
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Combine(Settings.GetExecutableDirectory() ?? string.Empty, "crash3.txt"), (ex.Message + "\n" + ex.StackTrace));
        }
        
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UseSkia()
                .WithInterFont()
                .LogToTrace();

            if (OperatingSystem.IsFreeBSD())
            {
                // --- FREEBSD (Critter-Net) CONFIGURATION ---
                // Forces the specific settings needed for headless Xpra/SSH
                builder.UseX11().With(new X11PlatformOptions 
                { 
                    UseGLibMainLoop = true,            // The fix for the event loop hang
                    RenderingMode = new [] { X11RenderingMode.Vulkan }, // Prevents GPU hang
                    UseDBusMenu = false                // Prevents DBus hang
                });
            }
            else
            {
                // --- WINDOWS / LINUX / MAC CONFIGURATION ---
                // Uses the standard auto-detection (Win32 on Windows, X11/Wayland on Linux)
                builder.UsePlatformDetect();
            }

            return builder;
        }
        
        /*private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                /*.UsePlatformDetect()#1#
                .UseX11()
                .With(new X11PlatformOptions 
                { 
                    // CRITICAL: Stops waiting for V-Sync (Fixes frozen UI)
                    UseGLibMainLoop = true,

                    // Force Software Rendering
                    RenderingMode = new [] { X11RenderingMode.Software },

                    // Disable DBus to prevent hangs
                    UseDBusMenu = false 
                })
                .UseSkia()
                /*.WithInterFont()#1#
                .LogToTrace();*/
    }
}
