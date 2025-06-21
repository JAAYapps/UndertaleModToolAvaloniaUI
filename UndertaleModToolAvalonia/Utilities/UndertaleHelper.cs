using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Win32;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia.Utilities
{
    public static class UndertaleHelper
    {
        private static List<(GMImage, WeakReference<Bitmap>)> _bitmapSourceLookup { get; } = new();
        private static object _bitmapSourceLookupLock = new();

        public static bool IsGMS2 => (AppConstants.Data?.GeneralInfo?.Major ?? 0) >= 2 ? true : false;
        
        // God this is so ugly, if there's a better way, please, put in a pull request
        public static bool IsExtProductIDEligible => (AppConstants.Data?.GeneralInfo?.Major ?? 0) >= 2 || (AppConstants.Data?.GeneralInfo?.Major ?? 0) == 1 && ((AppConstants.Data?.GeneralInfo?.Build ?? 0) >= 1773 || (AppConstants.Data?.GeneralInfo?.Build ?? 0) == 1539) ? true : false;
        
        public static bool GMLCacheEnabled => Settings.Instance.UseGMLCache;
        
        private static string ExePath { get; } = Program.GetExecutableDirectory();
        
        // For delivering messages to LoaderDialogs
        public delegate void FileMessageEventHandler(string message);
        public static event FileMessageEventHandler FileMessageEvent;
        
        /// <summary>
        /// Returns a <see cref="BitmapSource"/> instance for the given <see cref="GMImage"/>.
        /// If a previously-created instance has not yet been garbage collected, this will return that instance.
        /// </summary>
        public static Bitmap GetBitmapSourceForImage(GMImage image)
        {
            lock (_bitmapSourceLookupLock)
            {
                // Look through entire list, clearing out old weak references, and potentially finding our desired source
                Bitmap foundSource = null;
                for (int i = _bitmapSourceLookup.Count - 1; i >= 0; i--)
                {
                    (GMImage imageKey, WeakReference<Bitmap> referenceVal) = _bitmapSourceLookup[i];
                    if (!referenceVal.TryGetTarget(out Bitmap source))
                    {
                        // Clear out old weak reference
                        _bitmapSourceLookup.RemoveAt(i);
                    }
                    else if (imageKey == image)
                    {
                        // Found our source, store it to return later
                        foundSource = source;
                    }
                }

                // If we found our source, return it
                if (foundSource is not null)
                {
                    return foundSource;
                }

                // If no source was found, then create a new one
                byte[] pixelData = image.ConvertToRawBgra().ToSpan().ToArray();
                //Bitmap bitmap = new Bitmap(new Avalonia.PixelSize(image.Width, image.Height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);   //(image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null, pixelData, image.Width * 4);
                
                GCHandle pinnedArray = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                nint pointer = pinnedArray.AddrOfPinnedObject();
                Bitmap bitmap = new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Unpremul, pointer, new PixelSize(image.Width, image.Height), new Vector(96, 96), image.Width * 4);
                pinnedArray.Free();
                _bitmapSourceLookup.Add((image, new WeakReference<Bitmap>(bitmap)));
                return bitmap;
            }
        }

        public static void OpenFolder(string folder)
        {
            if (!folder.EndsWith(Path.DirectorySeparatorChar))
                folder += Path.DirectorySeparatorChar;

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folder,
                    UseShellExecute = true,
                    Verb = "Open"
                });
            }
            catch (Exception e)
            {
                _ = Application.Current.ShowError("Failed to open folder!\n" + e);
            }
        }

        public static Dictionary<string, NamedPipeServerStream> childFiles = new Dictionary<string, NamedPipeServerStream>();

        public static void OpenChildFile(string filename, string chunkName, int itemIndex)
        {
            if (childFiles.ContainsKey(filename))
            {
                try
                {
                    StreamWriter existingwriter = new StreamWriter(childFiles[filename]);
                    existingwriter.WriteLine(chunkName + ":" + itemIndex);
                    existingwriter.Flush();
                    return;
                }
                catch (IOException e)
                {
                    Debug.WriteLine(e);
                    childFiles.Remove(filename);
                }
            }

            string key = Guid.NewGuid().ToString();

            string dir = Path.GetDirectoryName(AppConstants.FilePath);
            Process.Start(Environment.ProcessPath, "\"" + Path.Combine(dir, filename) + "\" " + key);

            var server = new NamedPipeServerStream(key);
            server.WaitForConnection();
            childFiles.Add(filename, server);

            StreamWriter writer = new StreamWriter(childFiles[filename]);
            writer.WriteLine(chunkName + ":" + itemIndex);
            writer.Flush();
        }

        public static void CloseChildFiles()
        {
            foreach (var pair in childFiles)
            {
                pair.Value.Close();
            }
            childFiles.Clear();
        }
        
        public static async Task ListenChildConnection(string key)
        {
            var client = new NamedPipeClientStream(key);
            client.Connect();
            StreamReader reader = new StreamReader(client);

            while (true)
            {
                string[] thingToOpen = (await reader.ReadLineAsync()).Split(':');
                if (thingToOpen.Length != 2)
                    throw new Exception("ummmmm");
                if (thingToOpen[0] != "AUDO") // Just pretend I'm not hacking it together that poorly
                    throw new Exception("errrrr");
                // TODO OpenInTab(AppConstants.Data.EmbeddedAudio[Int32.Parse(thingToOpen[1])], false, "Embedded Audio");
                // Activate();
            }
        }

        public static async Task OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using (var process = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "/bin/sh",
                            Arguments = $"-c \"{$"xdg-open {url}".Replace("\"", "\\\"")}\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    )) { }
                }
                else
                {
                    using (var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                        Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                        CreateNoWindow = true,
                        UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    })) { }
                }
            }
            catch (Exception e)
            {
                await Application.Current.ShowError("Failed to open browser!\n" + e);
            }
        }
    }
}
