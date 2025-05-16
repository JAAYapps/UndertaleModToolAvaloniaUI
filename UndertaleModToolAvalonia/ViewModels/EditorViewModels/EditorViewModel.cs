using Avalonia.Controls;
using Avalonia.Data;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using SystemJson = System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using UndertaleModToolAvalonia.Utility;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews;
using static UndertaleModLib.Compiler.Compiler.Lexer;
using UndertaleModLib.Util;
using UndertaleModToolUniversal.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using UndertaleModToolAvalonia.Converters;
using UndertaleModToolAvalonia.Models.EditorModels;
using Boolean = System.Boolean;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class EditorViewModel : ViewModelBase
    {
        [ObservableProperty] private int currentTabIndex = 0;

        [ObservableProperty] private object highlighted;

        public object Selected
        {
            get => null;// TODO Implement CurrentTab?.CurrentObject;
            set
            {
                OnPropertyChanged();
                // TODO Implement OpenInTab(value);
            } 
        }
        
        [ObservableProperty] private bool wasWarnedAboutTempRun = false;
        
        [ObservableProperty] private bool finishedMessageEnabled = true;
        
        [ObservableProperty] private bool scriptExecutionSuccess = true;
        
        [ObservableProperty] private bool isSaving = false;
        
        [ObservableProperty] private string scriptErrorMessage = "";
        
        [ObservableProperty] private string scriptErrorType = "";
        
        public enum SaveResult
        {
            NotSaved,
            Saved,
            Error
        }
        public enum ScrollDirection
        {
            Left,
            Right
        }
        
        private int progressValue;
        private Task updater;
        private CancellationTokenSource cts;
        private CancellationToken cToken;
        private readonly object bindingLock = new();
        private HashSet<string> syncBindings = new();
        [ObservableProperty] private bool roomRendererEnabled;

        partial void OnRoomRendererEnabledChanged(bool value)
        {
            // TODO Add Avalonia implementation.
            // if (UndertaleRoomRenderer.RoomRendererTemplate is null)
            //     UndertaleRoomRenderer.RoomRendererTemplate = (DataTemplate)DataEditor.FindResource("roomRendererTemplate");

            if (value)
            {
                // DataEditor.ContentTemplate = UndertaleRoomRenderer.RoomRendererTemplate;
                UndertaleCachedImageLoader.ReuseTileBuffer = true;
            }
            else
            {
                // DataEditor.ContentTemplate = null;
                // CurrentTab.CurrentObject = LastOpenedObject;
                // LastOpenedObject = null;
                UndertaleCachedImageLoader.Reset();
                // CachedTileDataLoader.Reset();
            }
        }

        [ObservableProperty] private object lastOpenedObject; // for restoring the object that was opened before room rendering
        
        [ObservableProperty] private bool lsAppClosed = false;
        
        private HttpClient httpClient;
        
        private LoaderDialogView scriptDialog;
        
        public static string AppDataFolder => Settings.AppDataFolder;
        public static string ProfilesFolder = Path.Combine(Settings.AppDataFolder, "Profiles");
        public static string CorrectionsFolder = Path.Combine(Program.GetExecutableDirectory(), "Corrections");
        public string ProfileHash = "Unknown";
        public bool CrashedWhileEditing = false;

        // Scripting interface-related
        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;
        
        [ObservableProperty] private string objectLabel  = string.Empty;
        
        // TODO Not sure if I can just use Avalonia's theme system.
        // private static readonly Color darkColor = Color.FromArgb(255, 32, 32, 32);
        // private static readonly Color darkLightColor = Color.FromArgb(255, 48, 48, 48);
        // private static readonly Color whiteColor = Color.FromArgb(255, 222, 222, 222);
        // private static readonly SolidColorBrush grayTextBrush = new(Color.FromArgb(255, 179, 179, 179));
        // private static readonly SolidColorBrush inactiveSelectionBrush = new(Color.FromArgb(255, 212, 212, 212));
        // private static readonly Dictionary<ResourceKey, object> appDarkStyle = new()
        // {
        //     { SystemColors.WindowTextBrushKey, new SolidColorBrush(whiteColor) },
        //     { SystemColors.ControlTextBrushKey, new SolidColorBrush(whiteColor) },
        //     { SystemColors.WindowBrushKey, new SolidColorBrush(darkColor) },
        //     { SystemColors.ControlBrushKey, new SolidColorBrush(darkLightColor) },
        //     { SystemColors.ControlLightBrushKey, new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) },
        //     { SystemColors.MenuTextBrushKey, new SolidColorBrush(whiteColor) },
        //     { SystemColors.MenuBrushKey, new SolidColorBrush(darkLightColor) },
        //     { SystemColors.GrayTextBrushKey, new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)) },
        //     { SystemColors.InactiveSelectionHighlightBrushKey, new SolidColorBrush(Color.FromArgb(255, 112, 112, 112)) }
        // };

        public EditorViewModel()
        {
            Highlighted = new Description("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            // TODO Implement OpenInTab(Highlighted);

            _ = new Settings();

            Settings.Instance.CanSave = false;
            Settings.Instance.CanSafelySave = false;

            scriptSetupTask = Task.Run(() =>
            {
                scriptOptions = ScriptOptions.Default
                    .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                        "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                        "UndertaleModToolAvalonia", "System", "System.IO", "System.Collections.Generic",
                        "System.Text.RegularExpressions")
                    .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                        GetType().GetTypeInfo().Assembly,
                        typeof(JsonConvert).GetTypeInfo().Assembly,
                        typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly)
                    .WithEmitDebugInformation(true); //when script throws an exception, add an exception location (line number)
            });

            // TODO Same thing about Avalonia theme system.
            // var resources = Application.Current.Resources;
            // resources["CustomTextBrush"] = SystemColors.ControlTextBrush;
            // resources[SystemColors.GrayTextBrushKey] = grayTextBrush;
            // resources[SystemColors.InactiveSelectionHighlightBrushKey] = inactiveSelectionBrush;
        }
        
        
        
        
        
        
        
        
        

        
        private readonly Window perent;

        public EditorViewModel(Window perent)
        {
            this.perent = perent;
        }

        // [RelayCommand]
        // public async Task<bool> SaveDialog(bool suppressDebug = false)
        // {
        //     SaveFileDialog dlg = new SaveFileDialog();
        //     dlg.DefaultExtension = "wim";
        //     List<FileDialogFilter> filters = new List<FileDialogFilter>();
        //     FileDialogFilter AllFilter = new FileDialogFilter();
        //     AllFilter.Name = "All files";
        //     List<string> all = new List<string>();
        //     all.Add("*");
        //     AllFilter.Extensions = all;
        //     FileDialogFilter fileFilter = new FileDialogFilter();
        //     fileFilter.Name = "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)||*";
        //     List<string> ext = new List<string>();
        //     ext.Add("*.win");
        //     ext.Add("*.unx");
        //     ext.Add("*.ios");
        //     ext.Add("*.droid");
        //     ext.Add("audiogroup*.dat");
        //     fileFilter.Extensions = ext;
        //     filters.Add(fileFilter);
        //
        //     dlg.Filters = filters;
        //     dlg.Directory = AppConstants.LOCATION;
        //     string? file = await dlg.ShowAsync(perent);
        //     if (file != null)
        //     {
        //         await SaveFile(file, suppressDebug);
        //         return true;
        //     }
        //     return false;
        // }
        //
        // [RelayCommand]
        // public async Task<bool> OpenDialog()
        // {
        //     
        //     OpenFileDialog dlg = new OpenFileDialog();
        //     dlg.AllowMultiple = false;
        //     List<FileDialogFilter> filters = new List<FileDialogFilter>();
        //     FileDialogFilter AllFilter = new FileDialogFilter();
        //     AllFilter.Name = "All files";
        //     List<string> all = new List<string>();
        //     all.Add("");
        //     AllFilter.Extensions = all;
        //     FileDialogFilter fileFilter = new FileDialogFilter();
        //     fileFilter.Name = "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)";
        //     List<string> ext = new List<string>();
        //     ext.Add("win");
        //     ext.Add("unx");
        //     ext.Add("ios");
        //     ext.Add("droid");
        //     ext.Add("audiogroup*.dat");
        //     fileFilter.Extensions = ext;
        //     filters.Add(fileFilter);
        //     filters.Add(AllFilter);
        //     dlg.Filters = filters;
        //     dlg.Directory = AppConstants.LOCATION;
        //     string[]? file = await dlg.ShowAsync(perent);
        //     if (file != null)
        //     {
        //         await LoadFile(file[0], true);
        //         return true;
        //     }
        //     return false;
        // }

        // public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, bool clearGMLEditedBefore = false)
        // {
        //     if (!ProfileViewModel.UseGMLCache)
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
        //     if (!LoaderDialogFactory.Created)
        //     {
        //         await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        //         {
        //             await LoaderDialogFactory.Create(perent, true, "Script in progress...", "Please wait...");
        //             LoaderDialogFactory.HideProgressBar();
        //         });
        //     }
        //
        //     if (decompileContext is null)
        //         decompileContext = new(() => new GlobalDecompileContext(AppConstants.Data, false));
        //
        //     if (AppConstants.Data.KnownSubFunctions is null) //if we run script before opening any code
        //         Decompiler.BuildSubFunctionCache(AppConstants.Data);
        //
        //     if (AppConstants.Data.GMLCache.IsEmpty)
        //     {
        //         await LoaderDialogFactory.SetProgressBar(null, "Generating decompiled code cache...", 0, AppConstants.Data.Code.Count);
        //         LoaderDialogFactory.StartProgressBarUpdater();
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
        //                     AppConstants.Data.Code.Where(x => x.ParentEntry is null)
        //                          .Select(x => x.Name.Content)
        //                          .Except(AppConstants.Data.GMLCache.Keys)
        //                          .Except(AppConstants.Data.GMLCacheFailed))
        //                 .ToList();
        //         }
        //
        //         if (codeToUpdate.Count > 0)
        //         {
        //             await LoaderDialogFactory.SetProgressBar(null, "Updating decompiled code cache...", 0, codeToUpdate.Count);
        //             LoaderDialogFactory.StartProgressBarUpdater();
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
        //             LoaderDialogFactory.Dispose();
        //
        //         if (createdDialog)
        //         {
        //             await LoaderDialogFactory.StopProgressBarUpdater();
        //             LoaderDialogFactory.HideProgressBar();
        //         }
        //     }
        //
        //     AppConstants.Data.GMLCacheIsReady = true;
        //
        //     return true;
        // }
        //
        // private async Task LoadGMLCache(string filename)
        // {
        //     await Task.Run(async () => {
        //         if (ProfileViewModel.UseGMLCache)
        //         {
        //             string cacheDirPath = Path.Combine(ExePath, "GMLCache");
        //             string cacheIndexPath = Path.Combine(cacheDirPath, "index");
        //
        //             if (!File.Exists(cacheIndexPath))
        //                 return;
        //
        //             await Dispatcher.UIThread.InvokeAsync(async () => await LoaderDialogFactory.UpdateProgressStatus("Loading decompiled code cache..."));
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
        //                 Application.Current.ShowMessage("Decompiled code cache file for open data is missing, but its name present in the index.");
        //
        //                 return;
        //             }
        //
        //             string hash = MD5HashManager.GenerateMD5(filename);
        //
        //             using (StreamReader fs = new(Path.Combine(cacheDirPath, num.ToString())))
        //             {
        //                 string prevHash = fs.ReadLine();
        //
        //                 if (!Regex.IsMatch(prevHash, "^[0-9a-fA-F]{32}$")) //if first 32 bytes of cache file are not a valid MD5
        //                     Application.Current.ShowMessage("Decompiled code cache for open file is broken.\nThe cache will be generated again.");
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
        //                             Application.Current.ShowMessage("Decompiled code cache for open file is broken.\nThe cache will be generated again.");
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
        //                             Application.Current.ShowMessage($"Decompiled code cache for open file contains one or more non-existent code names (first - \"{invalidNames[0]}\").\nThe cache will be generated again.");
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
        //                         Application.Current.ShowMessage("Open file differs from the one the cache was generated for.\nThat decompiled code cache will be generated again.");
        //                 }
        //             }
        //         }
        //     });
        // }
        //
        // private async Task SaveGMLCache(string filename, bool updateCache = true, bool isDifferentPath = false)
        // {
        //     await Task.Run(async () =>
        //     {
        //         if (ProfileViewModel.UseGMLCache && AppConstants.Data?.GMLCache?.Count > 0 && AppConstants.Data.GMLCacheIsReady && (isDifferentPath || !AppConstants.Data.GMLCacheWasSaved || !AppConstants.Data.GMLCacheChanged.IsEmpty))
        //         {
        //             await Dispatcher.UIThread.InvokeAsync(() => LoaderDialogFactory.UpdateProgressStatus("Saving decompiled code cache..."));
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
        //                 await GenerateGMLCache(null, true);
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
        //             await Dispatcher.UIThread.InvokeAsync(() => LoaderDialogFactory.UpdateProgressStatus("Saving decompiled code cache..."));
        //
        //             string hash = MD5HashManager.GenerateMD5(filename);
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
        //
        // public void CreateUMTLastEdited(string filename)
        // {
        //     try
        //     {
        //         File.WriteAllText(Path.Combine(ProfilesFolder, "LastEdited.txt"), ProfileHash + "\n" + filename);
        //     }
        //     catch (Exception exc)
        //     {
        //         Application.Current.ShowMessage("CreateUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
        //     }
        // }
        //
        // public void DestroyUMTLastEdited()
        // {
        //     try
        //     {
        //         string path = Path.Combine(ProfilesFolder, "LastEdited.txt");
        //         if (File.Exists(path))
        //             File.Delete(path);
        //     }
        //     catch (Exception exc)
        //     {
        //         Application.Current.ShowMessage("DestroyUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
        //     }
        // }

        

        // private async Task LoadFile(string filename, bool preventClose = false)
        // {
        //     await LoaderDialogFactory.Create(perent, preventClose, "Loading", "Loading, please wait...");
        //     DisposeGameData();
        //     Task t = Task.Run(() =>
        //     {
        //         bool hadWarnings = false;
        //         UndertaleData data = null;
        //         try
        //         {
        //             using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
        //             {
        //                 data = UndertaleIO.Read(stream, warning =>
        //                 {
        //                     MessageBox.Show($"Loading warning\n{warning}", perent);
        //                     hadWarnings = true;
        //                 }, async message =>
        //                 {
        //                     await LoaderDialogFactory.UpdateProgressStatus(message);
        //                 });
        //             }
        //
        //             // UndertaleEmbeddedTexture.TexData.ClearSharedStream();
        //         }
        //         catch (Exception e)
        //         {
        //             MessageBox.Show("An error occured while trying to load:\n" + e.Message);
        //         }
        //
        //         Dispatcher.UIThread.InvokeAsync(async () =>
        //         {
        //             if (data != null)
        //             {
        //                 if (data.UnsupportedBytecodeVersion)
        //                 {
        //                     MessageBox.Show("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.");
        //                     CanSave = false;
        //                     CanSafelySave = false;
        //                 }
        //                 else if (hadWarnings)
        //                 {
        //                     MessageBox.Show("Warnings occurred during loading. Data loss will likely occur when trying to save!");
        //                     CanSave = true;
        //                     CanSafelySave = false;
        //                 }
        //                 else
        //                 {
        //                     CanSave = true;
        //                     CanSafelySave = true;
        //                     await UpdateProfile(data, filename);
        //                     if (data != null)
        //                     {
        //                         data.ToolInfo.ProfileMode = ProfileViewModel.ProfileModeEnabled;
        //                         data.ToolInfo.CurrentMD5 = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
        //                     }
        //                 }
        //                 if (data.IsYYC())
        //                 {
        //                     MessageBox.Show("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.");
        //                 }
        //                 if (data.GeneralInfo != null)
        //                 {
        //                     if (!data.GeneralInfo.IsDebuggerDisabled)
        //                     {
        //                         MessageBox.Show("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.");
        //                     }
        //                 }
        //                 if (Path.GetDirectoryName(FilePath) != Path.GetDirectoryName(filename))
        //                     CloseChildFiles();
        //
        //                 if (FilePath != filename)
        //                     await SaveGMLCache(FilePath, false);
        //
        //                 Data = data;
        //
        //                 await LoadGMLCache(filename);
        //                 UndertaleCachedImageLoader.Reset();
        //                 //                CachedTileDataLoader.Reset();
        //
        //                 //                Data.ToolInfo.AppDataProfiles = ProfilesFolder;
        //                 //                FilePath = filename;
        //                 //                OnPropertyChanged("Data");
        //                 //                OnPropertyChanged("FilePath");
        //                 //                OnPropertyChanged("IsGMS2");
        //
        //                 //                BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
        //                 //                                              ? "Tile sets"
        //                 //                                              : "Backgrounds & Tile sets";
        //
        //                 //#pragma warning disable CA1416
        //                 //                UndertaleCodeEditor.gettext = null;
        //                 //                UndertaleCodeEditor.gettextJSON = null;
        //                 //#pragma warning restore CA1416
        //             }
        //             LoaderDialogFactory.HideProgressBar();
        //         });
        //     });
        //     await t;
        //
        //     // Clear "GC holes" left in the memory in process of data unserializing
        //     // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
        //     GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        //     GC.Collect();
        // }
        //
        // private async Task SaveFile(string filename, bool suppressDebug = false)
        // {
        //
        // }
    }
}
