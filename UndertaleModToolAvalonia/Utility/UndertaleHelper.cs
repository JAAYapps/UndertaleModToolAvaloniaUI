using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Win32;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;

namespace UndertaleModToolAvalonia.Utility
{
    public static class UndertaleHelper
    {
        private static List<(GMImage, WeakReference<Bitmap>)> _bitmapSourceLookup { get; } = new();
        private static object _bitmapSourceLookupLock = new();

        public static bool IsGMS2 => (AppConstants.Data?.GeneralInfo?.Major ?? 0) >= 2 ? true : false;
        
        // God this is so ugly, if there's a better way, please, put in a pull request
        public static bool IsExtProductIDEligible => (((AppConstants.Data?.GeneralInfo?.Major ?? 0) >= 2) || (((AppConstants.Data?.GeneralInfo?.Major ?? 0) == 1) && (((AppConstants.Data?.GeneralInfo?.Build ?? 0) >= 1773) || ((AppConstants.Data?.GeneralInfo?.Build ?? 0) == 1539)))) ? true : false;
        
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
                IntPtr pointer = pinnedArray.AddrOfPinnedObject();
                Bitmap bitmap = new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Unpremul, pointer, new PixelSize(image.Width, image.Height), new Vector(96, 96), image.Width * 4);
                pinnedArray.Free();
                _bitmapSourceLookup.Add((image, new WeakReference<Bitmap>(bitmap)));
                return bitmap;
            }
        }
        
        private static void DisposeGameData()
        {
            if (AppConstants.Data is not null)
            {
                // TODO Implement the tabs
                // This also clears all their game object references
                // CurrentTab = null;
                // Tabs.Clear();
                // ClosedTabsHistory.Clear();

                // Update GUI and wait for all background processes to finish
                Dispatcher.UIThread.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                AppConstants.Data.Dispose();
                AppConstants.Data = null;

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
        }
        
        
//         private static async Task LoadFile(string filename, bool preventClose = false, bool onlyGeneralInfo = false)
//         {
//             LoaderDialogFactory.Create(Application.Current?.ApplicationLifetime as Window, preventClose, "Loading", "Loading, please wait...");
//
//             DisposeGameData();
//             // Highlighted = new Description("Welcome to UndertaleModTool!", "Double click on the items on the left to view them!");
//             // OpenInTab(Highlighted);
//
//             Task t = Task.Run(() =>
//             {
//                 bool hadWarnings = false;
//                 UndertaleData data = null;
//                 try
//                 {
//                     using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
//                     {
//                         data = UndertaleIO.Read(stream, warning =>
//                         {
//                             Application.Current.ShowWarning(warning, "Loading warning");
//                             if (warning.Contains("unserializeCountError.txt")
//                                 || warning.Contains("object pool size"))
//                                 return;
//
//                             hadWarnings = true;
//                         }, message =>
//                         {
//                             FileMessageEvent?.Invoke(message);
//                         }, onlyGeneralInfo);
//                     }
//                 }
//                 catch (Exception e)
//                 {
// #if DEBUG
//                     Debug.WriteLine(e);
// #endif
//                     Application.Current.ShowError("An error occured while trying to load:\n" + e.Message, "Load error");
//                 }
//
//                 if (onlyGeneralInfo)
//                 {
//                     Dispatcher.UIThread.Invoke(() =>
//                     {
//                         LoaderDialogFactory.Hide();
//                         AppConstants.Data = data;
//                         AppConstants.FilePath = filename;
//                     });
//
//                     return;
//                 }
//
//                 Dispatcher.UIThread.InvokeAsync(() =>
//                 {
//                     if (data != null)
//                     {
//                         if (data.UnsupportedBytecodeVersion)
//                         {
//                             Application.Current.ShowWarning("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version");
//                             Settings.Instance.CanSave = false;
//                             Settings.Instance.CanSafelySave = false;
//                         }
//                         else if (hadWarnings)
//                         {
//                             Application.Current.ShowWarning("Warnings occurred during loading. Data loss will likely occur when trying to save!", "Loading problems");
//                             Settings.Instance.CanSave = true;
//                             Settings.Instance.CanSafelySave = false;
//                         }
//                         else
//                         {
//                             Settings.Instance.CanSave = true;
//                             Settings.Instance.CanSafelySave = true;
//                             UpdateProfile(data, filename);
//                             if (data != null)
//                             {
//                                 data.ToolInfo.ProfileMode = Settings.Instance.ProfileModeEnabled;
//                                 data.ToolInfo.CurrentMD5 = BitConverter.ToString(Settings.Instance.MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
//                             }
//                         }
//                         if (data.IsYYC())
//                         {
//                             Application.Current.ShowWarning("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC");
//                         }
//                         if (data.GeneralInfo != null)
//                         {
//                             if (!data.GeneralInfo.IsDebuggerDisabled)
//                             {
//                                 Application.Current.ShowWarning("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
//                             }
//                         }
//                         if (Path.GetDirectoryName(AppConstants.FilePath) != Path.GetDirectoryName(filename))
//                             CloseChildFiles();
//
//                         if (AppConstants.FilePath != filename)
//                             SaveGMLCache(AppConstants.FilePath, false, dialog);
//
//                         AppConstants.Data = data;
//
//                         LoadGMLCache(filename, dialog);
//                         UndertaleCachedImageLoader.Reset();
//                         CachedTileDataLoader.Reset();
//
//                         AppConstants.Data.ToolInfo.AppDataProfiles = Settings.ProfilesFolder;
//                         AppConstants.FilePath = filename;
//                         // TODO Not sure if needed.
//                         // OnPropertyChanged("Data");
//                         // OnPropertyChanged("FilePath");
//                         // OnPropertyChanged("IsGMS2");
//
//                         BackgroundsItemsList.Header = IsGMS2 ? "Tile sets" : "Backgrounds & Tile sets";
//
//                         #pragma warning disable CA1416
//                         UndertaleCodeEditor.gettext = null;
//                         UndertaleCodeEditor.gettextJSON = null;
//                         #pragma warning restore CA1416
//                     }
//
//                     dialog.Hide();
//                 });
//             });
//             dialog.ShowDialog();
//             await t;
//
//             // Clear "GC holes" left in the memory in process of data unserializing
//             // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
//             GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
//             GC.Collect();
//         }
        //
        // private static async Task SaveFile(string filename, bool suppressDebug = false)
        // {
        //     if (AppConstants.Data == null || AppConstants.Data.UnsupportedBytecodeVersion)
        //         return;
        //
        //     bool isDifferentPath = AppConstants.FilePath != filename;
        //
        //     LoaderDialogFactory.Create(Application.Current?.ApplicationLifetime as Window, true, "Saving", "Saving, please wait...");
        //     
        //     IProgress<Tuple<int, string>> progress = new Progress<Tuple<int, string>>(i => { LoaderDialogFactory.ReportProgress(i.Item2, i.Item1); });
        //     IProgress<double?> setMax = new Progress<double?>(i => { dialog.Maximum = i; });
        //     
        //     AppConstants.FilePath = filename;
        //     
        //     if (Path.GetDirectoryName(AppConstants.FilePath) != Path.GetDirectoryName(filename))
        //         CloseChildFiles();
        //
        //     // TODO DebugDataDialog.DebugDataMode debugMode = DebugDataDialog.DebugDataMode.NoDebug;
        //     if (!suppressDebug && AppConstants.Data.GeneralInfo != null && !AppConstants.Data.GeneralInfo.IsDebuggerDisabled)
        //         Application.Current.ShowWarning("You are saving the game in GameMaker Studio debug mode. Unless the debugger is running, the normal runtime will simply hang after loading. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
        //     Task t = Task.Run(async () =>
        //     {
        //         bool SaveSucceeded = true;
        //
        //         try
        //         {
        //             using (var stream = new FileStream(filename + "temp", FileMode.Create, FileAccess.Write))
        //             {
        //                 UndertaleIO.Write(stream, AppConstants.Data, message =>
        //                 {
        //                     FileMessageEvent?.Invoke(message);
        //                 });
        //             }
        //
        //             QoiConverter.ClearSharedBuffer();
        //
        //             if (debugMode != DebugDataDialog.DebugDataMode.NoDebug)
        //             {
        //                 FileMessageEvent?.Invoke("Generating debugger data...");
        //
        //                 UndertaleDebugData debugData = UndertaleDebugData.CreateNew();
        //
        //                 setMax.Report(AppConstants.Data.Code.Count);
        //                 int count = 0;
        //                 object countLock = new object();
        //                 string[] outputs = new string[AppConstants.Data.Code.Count];
        //                 UndertaleDebugInfo[] outputsOffsets = new UndertaleDebugInfo[AppConstants.Data.Code.Count];
        //                 GlobalDecompileContext context = new GlobalDecompileContext(AppConstants.Data, false);
        //                 Parallel.For(0, AppConstants.Data.Code.Count, (i) =>
        //                 {
        //                     var code = AppConstants.Data.Code[i];
        //
        //                     if (debugMode == DebugDataDialog.DebugDataMode.Decompiled)
        //                     {
        //                         //Debug.WriteLine("Decompiling " + code.Name.Content);
        //                         string output;
        //                         try
        //                         {
        //                             output = Decompiler.Decompile(code, context);
        //                         }
        //                         catch (Exception e)
        //                         {
        //                             Debug.WriteLine(e.Message);
        //                             output = "/*\nEXCEPTION!\n" + e.ToString() + "\n*/";
        //                         }
        //                         outputs[i] = output;
        //
        //                         UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();
        //                         debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = 0, BytecodeOffset = 0 }); // TODO: generate this too! :D
        //                         outputsOffsets[i] = debugInfo;
        //                     }
        //                     else
        //                     {
        //                         StringBuilder sb = new StringBuilder();
        //                         UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();
        //
        //                         foreach (var instr in code.Instructions)
        //                         {
        //                             if (debugMode == DebugDataDialog.DebugDataMode.FullAssembler || instr.Kind == UndertaleInstruction.Opcode.Pop || instr.Kind == UndertaleInstruction.Opcode.Popz || instr.Kind == UndertaleInstruction.Opcode.B || instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf || instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
        //                                 debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = (uint)sb.Length, BytecodeOffset = instr.Address * 4 });
        //                             instr.ToString(sb, code);
        //                             sb.Append('\n');
        //                         }
        //                         outputs[i] = sb.ToString();
        //                         outputsOffsets[i] = debugInfo;
        //                     }
        //
        //                     lock (countLock)
        //                     {
        //                         progress.Report(new Tuple<int, string>(++count, code.Name.Content));
        //                     }
        //                 });
        //                 setMax.Report(null);
        //
        //                 for (int i = 0; i < AppConstants.Data.Code.Count; i++)
        //                 {
        //                     debugData.SourceCode.Add(new UndertaleScriptSource() { SourceCode = debugData.Strings.MakeString(outputs[i]) });
        //                     debugData.DebugInfo.Add(outputsOffsets[i]);
        //                     debugData.LocalVars.Add(AppConstants.Data.CodeLocals[i]);
        //                     if (debugData.Strings.IndexOf(AppConstants.Data.CodeLocals[i].Name) < 0)
        //                         debugData.Strings.Add(AppConstants.Data.CodeLocals[i].Name);
        //                     foreach (var local in AppConstants.Data.CodeLocals[i].Locals)
        //                         if (debugData.Strings.IndexOf(local.Name) < 0)
        //                             debugData.Strings.Add(local.Name);
        //                 }
        //
        //                 using (UndertaleWriter writer = new UndertaleWriter(new FileStream(Path.ChangeExtension(FilePath, ".yydebug"), FileMode.Create, FileAccess.Write)))
        //                 {
        //                     debugData.FORM.Serialize(writer);
        //                     writer.ThrowIfUnwrittenObjects();
        //                     writer.Flush();
        //                 }
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             if (!UndertaleIO.IsDictionaryCleared)
        //             {
        //                 try
        //                 {
        //                     var listChunks = AppConstants.Data.FORM.Chunks.Values.Select(x => x as IUndertaleListChunk);
        //                     Parallel.ForEach(listChunks.Where(x => x is not null), (chunk) =>
        //                     {
        //                         chunk.ClearIndexDict();
        //                     });
        //
        //                     UndertaleIO.IsDictionaryCleared = true;
        //                 }
        //                 catch { }
        //             }
        //
        //             Dispatcher.Invoke(() =>
        //             {
        //                 this.ShowError("An error occured while trying to save:\n" + e.Message, "Save error");
        //             });
        //
        //             SaveSucceeded = false;
        //         }
        //         // Don't make any changes unless the save succeeds.
        //         try
        //         {
        //             if (SaveSucceeded)
        //             {
        //                 // It saved successfully!
        //                 // If we're overwriting a previously existing data file, we're going to delete it now.
        //                 // Then, we're renaming it back to the proper (non-temp) file name.
        //                 if (File.Exists(filename))
        //                     File.Delete(filename);
        //                 File.Move(filename + "temp", filename);
        //
        //                 await SaveGMLCache(filename, true, dialog, isDifferentPath);
        //
        //                 // Also make the changes to the profile system.
        //                 await ProfileSaveEvent(AppConstants.Data, filename);
        //                 SaveTempToMainProfile();
        //             }
        //             else
        //             {
        //                 // It failed, but since we made a temp file for saving, no data was overwritten or destroyed (hopefully)
        //                 // We need to delete the temp file though (if it exists).
        //                 if (File.Exists(filename + "temp"))
        //                     File.Delete(filename + "temp");
        //                 // No profile system changes, since the save failed, like a save was never attempted.
        //             }
        //         }
        //         catch (Exception exc)
        //         {
        //             Dispatcher.Invoke(() =>
        //             {
        //                 this.ShowError("An error occured while trying to save:\n" + exc.Message, "Save error");
        //             });
        //
        //             SaveSucceeded = false;
        //         }
        //         if (AppConstants.Data != null)
        //         {
        //             AppConstants.Data.ToolInfo.ProfileMode = SettingsWindow.ProfileModeEnabled;
        //             AppConstants.Data.ToolInfo.CurrentMD5 = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
        //         }
        //
        //         #pragma warning disable CA1416
        //         UndertaleCodeEditor.gettextJSON = null;
        //         #pragma warning restore CA1416
        //
        //         Dispatcher.Invoke(() =>
        //         {
        //             dialog.Hide();
        //         });
        //     });
        //     dialog.ShowDialog();
        //     await t;
        //
        //     GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        //     GC.Collect();
        // }
        //
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
        
        // private static async void LoadGMLCache(string filename, LoaderDialogViewModel dialog = null)
        // {
        //     await Task.Run(() => {
        //         if (Settings.Instance.UseGMLCache)
        //         {
        //             string cacheDirPath = Path.Combine(ExePath, "GMLCache");
        //             string cacheIndexPath = Path.Combine(cacheDirPath, "index");
        //
        //             if (!File.Exists(cacheIndexPath))
        //                 return;
        //
        //             dialog?.Dispatcher.Invoke(() => dialog.ReportProgress("Loading decompiled code cache..."));
        //
        //             string[] indexLines = File.ReadAllLines(cacheIndexPath);
        //
        //             int num = -1;
        //             for (int i = 0; i < indexLines.Length; i++)
        //                 if (indexLines[i] == filename)
        //                 {
        //                     num = i;
        //                     break;
        //                 }
        //
        //             if (num == -1)
        //                 return;
        //
        //             if (!File.Exists(Path.Combine(cacheDirPath, num.ToString())))
        //             {
        //                 this.ShowWarning("Decompiled code cache file for open data is missing, but its name present in the index.");
        //
        //                 return;
        //             }
        //
        //             string hash = GenerateMD5(filename);
        //
        //             using (StreamReader fs = new(Path.Combine(cacheDirPath, num.ToString())))
        //             {
        //                 string prevHash = fs.ReadLine();
        //
        //                 if (!Regex.IsMatch(prevHash, "^[0-9a-fA-F]{32}$")) //if first 32 bytes of cache file are not a valid MD5
        //                     this.ShowWarning("Decompiled code cache for open file is broken.\nThe cache will be generated again.");
        //                 else
        //                 {
        //                     if (hash == prevHash)
        //                     {
        //                         string cacheStr = fs.ReadLine();
        //                         string failedStr = fs.ReadLine();
        //
        //                         try
        //                         {
        //                             AppConstants.Data.GMLCache = SystemJson.JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(cacheStr);
        //
        //                             if (failedStr is not null)
        //                                 AppConstants.Data.GMLCacheFailed = SystemJson.JsonSerializer.Deserialize<List<string>>(failedStr);
        //                             else
        //                                 AppConstants.Data.GMLCacheFailed = new();
        //                         }
        //                         catch
        //                         {
        //                             this.ShowWarning("Decompiled code cache for open file is broken.\nThe cache will be generated again.");
        //
        //                             AppConstants.Data.GMLCache = null;
        //                             AppConstants.Data.GMLCacheFailed = null;
        //
        //                             return;
        //                         }
        //
        //                         string[] codeNames = AppConstants.Data.Code.Where(x => x.ParentEntry is null).Select(x => x.Name.Content).ToArray();
        //                         string[] invalidNames = AppConstants.Data.GMLCache.Keys.Except(codeNames).ToArray();
        //                         if (invalidNames.Length > 0)
        //                         {
        //                             this.ShowWarning($"Decompiled code cache for open file contains one or more non-existent code names (first - \"{invalidNames[0]}\").\nThe cache will be generated again.");
        //
        //                             AppConstants.Data.GMLCache = null;
        //
        //                             return;
        //                         }
        //
        //                         AppConstants.Data.GMLCacheChanged = new();
        //                         AppConstants.Data.GMLEditedBefore = new();
        //                         AppConstants.Data.GMLCacheWasSaved = true;
        //                     }
        //                     else
        //                         this.ShowWarning("Open file differs from the one the cache was generated for.\nThat decompiled code cache will be generated again.");
        //                 }
        //             }
        //         }
        //     });
        // }
        // private static async void SaveGMLCache(string filename, bool updateCache = true, LoaderDialogViewModel dialog = null, bool isDifferentPath = false)
        // {
        //     await Task.Run(async () => {
        //         if (Settings.Instance.UseGMLCache && AppConstants.Data?.GMLCache?.Count > 0 && AppConstants.Data.GMLCacheIsReady && (isDifferentPath || !AppConstants.Data.GMLCacheWasSaved || !AppConstants.Data.GMLCacheChanged.IsEmpty))
        //         {
        //             Dispatcher.UIThread.Invoke(() => dialog.ReportProgress("Saving decompiled code cache..."));
        //
        //             string cacheDirPath = Path.Combine(ExePath, "GMLCache");
        //             string cacheIndexPath = Path.Combine(cacheDirPath, "index");
        //             if (!File.Exists(cacheIndexPath))
        //             {
        //                 Directory.CreateDirectory(cacheDirPath);
        //                 File.WriteAllText(cacheIndexPath, filename);
        //             }
        //
        //             List<string> indexLines = File.ReadAllLines(cacheIndexPath).ToList();
        //
        //             int num = -1;
        //             for (int i = 0; i < indexLines.Count; i++)
        //                 if (indexLines[i] == filename)
        //                 {
        //                     num = i;
        //                     break;
        //                 }
        //
        //             if (num == -1) //if it's new cache file
        //             {
        //                 num = indexLines.Count;
        //
        //                 indexLines.Add(filename);
        //             }
        //
        //             if (updateCache)
        //             {
        //                 await GenerateGMLCache(null, dialog, true);
        //                 await LoaderDialogFactory.StopProgressBarUpdater();
        //             }
        //
        //             string[] codeNames = AppConstants.Data.Code.Where(x => x.ParentEntry is null).Select(x => x.Name.Content).ToArray();
        //             Dictionary<string, string> sortedCache = new(AppConstants.Data.GMLCache.OrderBy(x => Array.IndexOf(codeNames, x.Key)));
        //             AppConstants.Data.GMLCacheFailed = AppConstants.Data.GMLCacheFailed.OrderBy(x => Array.IndexOf(codeNames, x)).ToList();
        //
        //             if (!updateCache && AppConstants.Data.GMLEditedBefore.Count > 0) //if saving the original cache
        //                 foreach (string name in AppConstants.Data.GMLEditedBefore)
        //                     sortedCache.Remove(name);                   //exclude the code that was edited from the save list
        //
        //             Dispatcher.UIThread.Invoke(() => dialog.ReportProgress("Saving decompiled code cache..."));
        //
        //             string hash = GenerateMD5(filename);
        //
        //             using (FileStream fs = File.Create(Path.Combine(cacheDirPath, num.ToString())))
        //             {
        //                 fs.Write(Encoding.UTF8.GetBytes(hash + '\n'));
        //                 fs.Write(SystemJson.JsonSerializer.SerializeToUtf8Bytes(sortedCache));
        //
        //                 if (AppConstants.Data.GMLCacheFailed.Count > 0)
        //                 {
        //                     fs.WriteByte((byte)'\n');
        //                     fs.Write(SystemJson.JsonSerializer.SerializeToUtf8Bytes(AppConstants.Data.GMLCacheFailed));
        //                 }
        //             }
        //
        //             File.WriteAllLines(cacheIndexPath, indexLines);
        //
        //             AppConstants.Data.GMLCacheWasSaved = true;
        //         }
        //     });
        // }

        // public static async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool clearGMLEditedBefore = false)
        // {
        //     if (!Settings.Instance.UseGMLCache)
        //         return false;
        //
        //     bool createdDialog = false;
        //     bool existedDialog = false;
        //     AppConstants.Data.GMLCacheIsReady = false;
        //
        //     if (AppConstants.Data.GMLCache is null)
        //         AppConstants.Data.GMLCache = new();
        //
        //     ConcurrentBag<string> failedBag = new();
        //
        //     if (scriptDialog is null)
        //     {
        //         if (dialog is null)
        //         {
        //             Dispatcher.Invoke(() =>
        //             {
        //                 scriptDialog = new LoaderDialog("Script in progress...", "Please wait...")
        //                 {
        //                     Owner = this,
        //                     PreventClose = true
        //                 };
        //             });
        //
        //             createdDialog = true;
        //         }
        //         else
        //             scriptDialog = dialog as LoaderDialog;
        //     }
        //     else
        //         existedDialog = true;
        //
        //     if (decompileContext is null)
        //         decompileContext = new(() => new GlobalDecompileContext(AppConstants.Data, false));
        //
        //     if (AppConstants.Data.KnownSubFunctions is null) //if we run script before opening any code
        //     {
        //         SetProgressBar(null, "Building the cache of all sub-functions...", 0, 0);
        //         await Task.Run(() => Decompiler.BuildSubFunctionCache(AppConstants.Data));
        //     }
        //
        //     if (AppConstants.Data.GMLCache.IsEmpty)
        //     {
        //         SetProgressBar(null, "Generating decompiled code cache...", 0, AppConstants.Data.Code.Count);
        //         StartProgressBarUpdater();
        //
        //         await Task.Run(() => Parallel.ForEach(AppConstants.Data.Code, (code) =>
        //         {
        //             if (code is not null && code.ParentEntry is null)
        //             {
        //                 try
        //                 {
        //                     AppConstants.Data.GMLCache[code.Name.Content] = Decompiler.Decompile(code, decompileContext.Value);
        //                 }
        //                 catch
        //                 {
        //                     failedBag.Add(code.Name.Content);
        //                 }
        //             }
        //
        //             IncrementProgressParallel();
        //         }));
        //
        //         AppConstants.Data.GMLEditedBefore = new(AppConstants.Data.GMLCacheChanged);
        //         AppConstants.Data.GMLCacheChanged.Clear();
        //         AppConstants.Data.GMLCacheFailed = failedBag.ToList();
        //     }
        //     else
        //     {
        //         List<string> codeToUpdate;
        //         bool cacheIsFull = !(AppConstants.Data.GMLCache.Count < AppConstants.Data.Code.Where(x => x.ParentEntry is null).Count() - AppConstants.Data.GMLCacheFailed.Count);
        //
        //         if (cacheIsFull)
        //         {
        //             AppConstants.Data.GMLCacheChanged = new(AppConstants.Data.GMLCacheChanged.Distinct()); //remove duplicates
        //
        //             codeToUpdate = AppConstants.Data.GMLCacheChanged.ToList();
        //         }
        //         else
        //         {
        //             //add missing and modified code cache names to the update list (and remove duplicates)
        //             codeToUpdate = AppConstants.Data.GMLCacheChanged.Union(
        //                 AppConstants.Data.Code.Where(x => x.ParentEntry is null)
        //                          .Select(x => x.Name.Content)
        //                          .Except(AppConstants.Data.GMLCache.Keys)
        //                          .Except(AppConstants.Data.GMLCacheFailed))
        //                 .ToList();
        //         }
        //
        //         if (codeToUpdate.Count > 0)
        //         {
        //             SetProgressBar(null, "Updating decompiled code cache...", 0, codeToUpdate.Count);
        //             StartProgressBarUpdater();
        //
        //             await Task.Run(() => Parallel.ForEach(codeToUpdate.Select(x => AppConstants.Data.Code.ByName(x)), (code) =>
        //             {
        //                 if (code is not null && code.ParentEntry is null)
        //                 {
        //                     try
        //                     {
        //                         AppConstants.Data.GMLCache[code.Name.Content] = Decompiler.Decompile(code, decompileContext.Value);
        //
        //                         AppConstants.Data.GMLCacheFailed.Remove(code.Name.Content); //that code compiles now
        //                     }
        //                     catch
        //                     {
        //                         failedBag.Add(code.Name.Content);
        //                     }
        //                 }
        //
        //                 IncrementProgressParallel();
        //             }));
        //
        //             if (clearGMLEditedBefore)
        //                 AppConstants.Data.GMLEditedBefore.Clear();
        //             else
        //                 AppConstants.Data.GMLEditedBefore = AppConstants.Data.GMLEditedBefore.Union(AppConstants.Data.GMLCacheChanged).ToList();
        //
        //             AppConstants.Data.GMLCacheChanged.Clear();
        //             AppConstants.Data.GMLCacheFailed = AppConstants.Data.GMLCacheFailed.Union(failedBag).ToList();
        //             AppConstants.Data.GMLCacheWasSaved = false;
        //         }
        //         else if (clearGMLEditedBefore)
        //             AppConstants.Data.GMLEditedBefore.Clear();
        //
        //         if (!existedDialog)
        //             scriptDialog = null;
        //
        //         if (createdDialog)
        //         {
        //             await StopProgressBarUpdater();
        //             HideProgressBar();
        //         }
        //     }
        //
        //     AppConstants.Data.GMLCacheIsReady = true;
        //
        //     return true;
        // }
        
        
        // This is the external method p/invoke for registering the mod tool in Windows as the program to open data.wim files for Undertale.
        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        const long SHCNE_ASSOCCHANGED = 0x08000000;
        
        public static readonly string[] IFF_EXTENSIONS = new string[] { ".win", ".unx", ".ios", ".droid", ".3ds", ".symbian" };
        
        public static async void Startup()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    string procFileName = Environment.ProcessPath;
                    var HKCU_Classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
                    var UndertaleModTool_app = HKCU_Classes.CreateSubKey(@"UndertaleModTool");

                    UndertaleModTool_app.SetValue("", "UndertaleModTool");
                    UndertaleModTool_app.CreateSubKey(@"shell\open\command").SetValue("", "\"" + procFileName + "\" \"%1\"", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\launch\command").SetValue("", "\"" + procFileName + "\" \"%1\" launch", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\launch").SetValue("", "Run game normally", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\special_launch\command").SetValue("", "\"" + procFileName + "\" \"%1\" special_launch", RegistryValueKind.String);
                    UndertaleModTool_app.CreateSubKey(@"shell\special_launch").SetValue("", "Run extended options", RegistryValueKind.String);

                    if (File.Exists("dna.txt"))
                    {
                        ScriptMessages.ScriptMessage("Opt out detected.");
                        Settings.Instance.AutomaticFileAssociation = false;
                        Settings.Save();
                    }
                    if (Settings.Instance.AutomaticFileAssociation)
                    {
                        foreach (var extStr in IFF_EXTENSIONS)
                        {
                            var ext = HKCU_Classes.CreateSubKey(extStr);
                            ext.SetValue("", "UndertaleModTool", RegistryValueKind.String);
                        }
                        SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            // var args = Environment.GetCommandLineArgs();
            // bool isLaunch = false;
            // bool isSpecialLaunch = false;
            // if (args.Length > 1)
            // {
            //     if (args.Length > 2)
            //     {
            //         isLaunch = args[2] == "launch";
            //         isSpecialLaunch = args[2] == "special_launch";
            //     }
            //
            //     string arg = args[1];
            //     if (File.Exists(arg))
            //     {
            //         await LoadFile(arg, true, isLaunch || isSpecialLaunch);
            //     }
            //     else if (arg == "deleteTempFolder") // if was launched from UndertaleModToolUpdater
            //     {
            //         _ = Task.Run(() =>
            //         {
            //             Process[] updaterInstances = Process.GetProcessesByName("UndertaleModToolUpdater");
            //             bool updaterClosed = false;
            //
            //             if (updaterInstances.Length > 0)
            //             {
            //                 foreach (Process instance in updaterInstances)
            //                 {
            //                     if (!instance.WaitForExit(5000))
            //                         Application.Current.ShowWarning("UndertaleModToolUpdater app didn't exit.\nCan't delete its temp folder.");
            //                     else
            //                         updaterClosed = true;
            //                 }
            //             }
            //             else
            //                 updaterClosed = true;
            //
            //             if (updaterClosed)
            //             {
            //                 bool deleted = false;
            //                 string exMessage = "(error message is missing)";
            //                 string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");
            //
            //                 for (int i = 0; i <= 5; i++)
            //                 {
            //                     try
            //                     {
            //                         Directory.Delete(tempFolder, true);
            //
            //                         deleted = true;
            //                         break;
            //                     }
            //                     catch (Exception ex)
            //                     {
            //                         exMessage = ex.Message;
            //                     }
            //
            //                     Thread.Sleep(1000);
            //                 }
            //
            //                 if (!deleted)
            //                     Application.Current.ShowWarning($"The updater temp folder can't be deleted.\nError - {exMessage}.");
            //             }
            //         });
            //     }
            //
            //     if (isSpecialLaunch)
            //     {
            //         RuntimePicker picker = new RuntimePicker();
            //         picker.Owner = this;
            //         var runtime = picker.Pick(AppConstants.FilePath, AppConstants.Data);
            //         if (runtime == null)
            //             return;
            //         Process.Start(runtime.Path, "-game \"" + AppConstants.FilePath + "\"");
            //         Environment.Exit(0);
            //     }
            //     else if (isLaunch)
            //     {
            //         string gameExeName = AppConstants.Data?.GeneralInfo?.FileName?.Content;
            //         if (gameExeName == null || AppConstants.FilePath == null)
            //         {
            //             ScriptMessages.ScriptError("Null game executable name or location");
            //             Environment.Exit(0);
            //         }
            //         string gameExePath = Path.Combine(Path.GetDirectoryName(AppConstants.FilePath), gameExeName + ".exe");
            //         if (!File.Exists(gameExePath))
            //         {
            //             ScriptMessages.ScriptError("Cannot find game executable path, expected: " + gameExePath);
            //             Environment.Exit(0);
            //         }
            //         if (!File.Exists(AppConstants.FilePath))
            //         {
            //             ScriptMessages.ScriptError("Cannot find data file path, expected: " + AppConstants.FilePath);
            //             Environment.Exit(0);
            //         }
            //         if (gameExeName != null)
            //             Process.Start(gameExePath, "-game \"" + AppConstants.FilePath + "\" -debugoutput \"" + Path.ChangeExtension(AppConstants.FilePath, ".gamelog.txt") + "\"");
            //         Environment.Exit(0);
            //     }
            //     else if (args.Length > 2)
            //     {
            //         _ = ListenChildConnection(args[2]);
            //     }
            // }

            // Copy the known code corrections into the profile, if they don't already exist.
            // ApplyCorrections();
            // CrashCheck();

            // RunGMSDebuggerItem.Visibility = ShowDebuggerOption ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
