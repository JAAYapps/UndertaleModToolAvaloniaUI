using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using UndertaleModToolAvalonia.Utility;
using UndertaleModToolAvalonia.Views;
using UndertaleModToolAvalonia.Views.EditorViews;
using static UndertaleModLib.Compiler.Compiler.Lexer;

namespace UndertaleModToolAvalonia.ViewModels.EditorsViewModels
{
    public class EditorViewModel : ViewModelBase
    {
        public enum CodeEditorMode
        {
            Unstated,
            DontDecompile,
            Decompile
        }
        public enum SaveResult
        {
            NotSaved,
            Saved,
            Error
        }

        private int progressValue;

        public bool IsEnabled { get; set; }

        public bool CanSave { get; set; }

        public bool CanSafelySave = false;

        public bool WasWarnedAboutTempRun = false;

        public bool FinishedMessageEnabled = true;

        public bool ScriptExecutionSuccess { get; set; } = true;

        public bool IsSaving { get; set; }

        public string ScriptErrorMessage { get; set; } = "";

        public string ExePath { get; private set; } = Program.GetExecutableDirectory();

        public string ScriptErrorType { get; set; } = "";

        public UndertaleData Data { get; set; }

        public string FilePath { get; set; }

        public string ScriptPath { get; set; } // For the scripting interface specifically

        public string TitleMain { get; set; }

        public Dictionary<string, NamedPipeServerStream> childFiles = new Dictionary<string, NamedPipeServerStream>();

        // Related to profile system and appdata
        public byte[] MD5PreviouslyLoaded = new byte[13];
        public byte[] MD5CurrentlyLoaded = new byte[15];
        public static string AppDataFolder => Settings.AppDataFolder;
        public static string ProfilesFolder = Path.Combine(Settings.AppDataFolder, "Profiles");
        public static string CorrectionsFolder = Path.Combine(Program.GetExecutableDirectory(), "Corrections");
        public string ProfileHash = "Unknown";
        public bool CrashedWhileEditing = false;

        private readonly Window perent;

        public ReactiveCommand<Unit, Task<bool>> DoOpenDialog { get; }

        public ReactiveCommand<bool, Task<bool>> DoSaveDialog { get; }

        public EditorViewModel(Window perent)
        {
            this.perent = perent;
            DoOpenDialog = ReactiveCommand.Create(OpenDialog);
            DoSaveDialog = ReactiveCommand.Create<bool, Task<bool>>(SaveDialog);
        }

        public void AddProgress(int amount)
        {
            progressValue += amount;
        }
        public void IncrementProgress()
        {
            progressValue++;
        }
        public void AddProgressParallel(int amount) //P - Parallel (multithreaded)
        {
            Interlocked.Add(ref progressValue, amount); //thread-safe add operation (not the same as "lock ()")
        }
        public void IncrementProgressParallel()
        {
            Interlocked.Increment(ref progressValue); //thread-safe increment
        }
        public int GetProgress()
        {
            return progressValue;
        }
        public void SetProgress(int value)
        {
            progressValue = value;
        }

        public void EnableUI()
        {
            if (!this.IsEnabled)
                this.IsEnabled = true;
        }

        public async Task<bool> SaveDialog(bool suppressDebug = false)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExtension = "wim";
            List<FileDialogFilter> filters = new List<FileDialogFilter>();
            FileDialogFilter AllFilter = new FileDialogFilter();
            AllFilter.Name = "All files";
            List<string> all = new List<string>();
            all.Add("*");
            AllFilter.Extensions = all;
            FileDialogFilter fileFilter = new FileDialogFilter();
            fileFilter.Name = "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)||*";
            List<string> ext = new List<string>();
            ext.Add("*.win");
            ext.Add("*.unx");
            ext.Add("*.ios");
            ext.Add("*.droid");
            ext.Add("audiogroup*.dat");
            fileFilter.Extensions = ext;
            filters.Add(fileFilter);

            dlg.Filters = filters;
            dlg.Directory = AppConstants.LOCATION;
            string? file = await dlg.ShowAsync(perent);
            if (file != null)
            {
                await SaveFile(file, suppressDebug);
                return true;
            }
            return false;
        }

        public async Task<bool> OpenDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AllowMultiple = false;
            List<FileDialogFilter> filters = new List<FileDialogFilter>();
            FileDialogFilter AllFilter = new FileDialogFilter();
            AllFilter.Name = "All files";
            List<string> all = new List<string>();
            all.Add("");
            AllFilter.Extensions = all;
            FileDialogFilter fileFilter = new FileDialogFilter();
            fileFilter.Name = "Game Maker Studio data files (.win, .unx, .ios, .droid, audiogroup*.dat)";
            List<string> ext = new List<string>();
            ext.Add("win");
            ext.Add("unx");
            ext.Add("ios");
            ext.Add("droid");
            ext.Add("audiogroup*.dat");
            fileFilter.Extensions = ext;
            filters.Add(fileFilter);
            filters.Add(AllFilter);
            dlg.Filters = filters;
            dlg.Directory = AppConstants.LOCATION;
            string[]? file = await dlg.ShowAsync(perent);
            if (file != null)
            {
                await LoadFile(file[0], true);
                return true;
            }
            return false;
        }

        private void DisposeGameData()
        {
            if (AppConstants.Data is not null)
            {
                AppConstants.Data.Dispose();
                AppConstants.Data = null;

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
        }

        public void CloseChildFiles()
        {
            foreach (var pair in childFiles)
            {
                pair.Value.Close();
            }
            childFiles.Clear();
        }

        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool clearGMLEditedBefore = false)
        {
            if (!ProfileViewModel.UseGMLCache)
                return false;

            bool createdDialog = false;
            bool existedDialog = false;
            Data.GMLCacheIsReady = false;

            if (Data.GMLCache is null)
                Data.GMLCache = new();

            ConcurrentBag<string> failedBag = new();

            if (scriptDialogViewModel is null)
            {
                if (dialog is null)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        scriptDialogView = await WindowLoader.createWindowAsync(perent,
                                            typeof(LoaderDialogView), typeof(LoaderDialogViewModel), true, "Script in progress...", "Please wait...");
                        scriptDialogViewModel = scriptDialogView.DataContext as LoaderDialogViewModel;
                    });

                    createdDialog = true;
                }
                else
                    scriptDialogViewModel = dialog as LoaderDialogViewModel;
            }
            else
                existedDialog = true;

            if (decompileContext is null)
                decompileContext = new(() => new GlobalDecompileContext(Data, false));

            if (Data.KnownSubFunctions is null) //if we run script before opening any code
                Decompiler.BuildSubFunctionCache(Data);

            if (Data.GMLCache.IsEmpty)
            {
                SetProgressBar(null, "Generating decompiled code cache...", 0, Data.Code.Count);
                StartProgressBarUpdater();

                await Task.Run(() => Parallel.ForEach(Data.Code, (code) =>
                {
                    if (code is not null && code.ParentEntry is null)
                    {
                        try
                        {
                            Data.GMLCache[code.Name.Content] = Decompiler.Decompile(code, decompileContext.Value);
                        }
                        catch
                        {
                            failedBag.Add(code.Name.Content);
                        }
                    }

                    IncrementProgressParallel();
                }));

                Data.GMLEditedBefore = new(Data.GMLCacheChanged);
                Data.GMLCacheChanged.Clear();
                Data.GMLCacheFailed = failedBag.ToList();
            }
            else
            {
                List<string> codeToUpdate;
                bool cacheIsFull = !(Data.GMLCache.Count < Data.Code.Where(x => x.ParentEntry is null).Count() - Data.GMLCacheFailed.Count);

                if (cacheIsFull)
                {
                    Data.GMLCacheChanged = new(Data.GMLCacheChanged.Distinct()); //remove duplicates

                    codeToUpdate = Data.GMLCacheChanged.ToList();
                }
                else
                {
                    //add missing and modified code cache names to the update list (and remove duplicates)
                    codeToUpdate = Data.GMLCacheChanged.Union(
                        Data.Code.Where(x => x.ParentEntry is null)
                                 .Select(x => x.Name.Content)
                                 .Except(Data.GMLCache.Keys)
                                 .Except(Data.GMLCacheFailed))
                        .ToList();
                }

                if (codeToUpdate.Count > 0)
                {
                    SetProgressBar(null, "Updating decompiled code cache...", 0, codeToUpdate.Count);
                    StartProgressBarUpdater();

                    await Task.Run(() => Parallel.ForEach(codeToUpdate.Select(x => Data.Code.ByName(x)), (code) =>
                    {
                        if (code is not null && code.ParentEntry is null)
                        {
                            try
                            {
                                Data.GMLCache[code.Name.Content] = Decompiler.Decompile(code, decompileContext.Value);

                                Data.GMLCacheFailed.Remove(code.Name.Content); //that code compiles now
                            }
                            catch
                            {
                                failedBag.Add(code.Name.Content);
                            }
                        }

                        IncrementProgressParallel();
                    }));

                    if (clearGMLEditedBefore)
                        Data.GMLEditedBefore.Clear();
                    else
                        Data.GMLEditedBefore = Data.GMLEditedBefore.Union(Data.GMLCacheChanged).ToList();

                    Data.GMLCacheChanged.Clear();
                    Data.GMLCacheFailed = Data.GMLCacheFailed.Union(failedBag).ToList();
                    Data.GMLCacheWasSaved = false;
                }
                else if (clearGMLEditedBefore)
                    Data.GMLEditedBefore.Clear();

                if (!existedDialog)
                    scriptDialog = null;

                if (createdDialog)
                {
                    await StopProgressBarUpdater();
                    HideProgressBar();
                }
            }

            Data.GMLCacheIsReady = true;

            return true;
        }

        private async Task SaveGMLCache(string filename, bool updateCache = true, LoaderDialogViewModel dialog = null, bool isDifferentPath = false)
        {
            await Task.Run(async () =>
            {
                if (ProfileViewModel.UseGMLCache && Data?.GMLCache?.Count > 0 && Data.GMLCacheIsReady && (isDifferentPath || !Data.GMLCacheWasSaved || !Data.GMLCacheChanged.IsEmpty))
                {
                    await Dispatcher.UIThread.InvokeAsync(() => dialog.ReportProgress("Saving decompiled code cache..."));

                    string cacheDirPath = Path.Combine(ExePath, "GMLCache");
                    string cacheIndexPath = Path.Combine(cacheDirPath, "index");
                    if (!File.Exists(cacheIndexPath))
                    {
                        Directory.CreateDirectory(cacheDirPath);
                        File.WriteAllText(cacheIndexPath, filename);
                    }

                    List<string> indexLines = File.ReadAllLines(cacheIndexPath).ToList();

                    int num = -1;
                    for (int i = 0; i < indexLines.Count; i++)
                        if (indexLines[i] == filename)
                        {
                            num = i;
                            break;
                        }

                    if (num == -1) //if it's new cache file
                    {
                        num = indexLines.Count;

                        indexLines.Add(filename);
                    }

                    if (updateCache)
                    {
                        await GenerateGMLCache(null, dialog, true);
                        await StopProgressBarUpdater();
                    }

                    string[] codeNames = Data.Code.Where(x => x.ParentEntry is null).Select(x => x.Name.Content).ToArray();
                    Dictionary<string, string> sortedCache = new(Data.GMLCache.OrderBy(x => Array.IndexOf(codeNames, x.Key)));
                    Data.GMLCacheFailed = Data.GMLCacheFailed.OrderBy(x => Array.IndexOf(codeNames, x)).ToList();

                    if (!updateCache && Data.GMLEditedBefore.Count > 0) //if saving the original cache
                        foreach (string name in Data.GMLEditedBefore)
                            sortedCache.Remove(name);                   //exclude the code that was edited from the save list

                    await Dispatcher.UIThread.InvokeAsync(() => dialog.ReportProgress("Saving decompiled code cache..."));

                    string hash = GenerateMD5(filename);

                    using (FileStream fs = File.Create(Path.Combine(cacheDirPath, num.ToString())))
                    {
                        fs.Write(Encoding.UTF8.GetBytes(hash + '\n'));
                        fs.Write(SystemJson.JsonSerializer.SerializeToUtf8Bytes(sortedCache));

                        if (Data.GMLCacheFailed.Count > 0)
                        {
                            fs.WriteByte((byte)'\n');
                            fs.Write(SystemJson.JsonSerializer.SerializeToUtf8Bytes(Data.GMLCacheFailed));
                        }
                    }

                    File.WriteAllLines(cacheIndexPath, indexLines);

                    Data.GMLCacheWasSaved = true;
                }
            });
        }

        public void CreateUMTLastEdited(string filename)
        {
            try
            {
                File.WriteAllText(Path.Combine(ProfilesFolder, "LastEdited.txt"), ProfileHash + "\n" + filename);
            }
            catch (Exception exc)
            {
                MessageBox.Show("CreateUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
            }
        }

        public void DestroyUMTLastEdited()
        {
            try
            {
                string path = Path.Combine(ProfilesFolder, "LastEdited.txt");
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exc)
            {
                MessageBox.Show("DestroyUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
            }
        }

        public async Task UpdateProfile(UndertaleData data, string filename)
        {
            await dialog.ReportProgress("Calculating MD5 hash...");

            try
            {
                await Task.Run(() =>
                {
                    using (var md5Instance = MD5.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                            MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                            ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                        }
                    }
                });

                string profDir = Path.Combine(ProfilesFolder, ProfileHash);
                string profDirTemp = Path.Combine(profDir, "Temp");
                string profDirMain = Path.Combine(profDir, "Main");

                if (ProfileViewModel.ProfileModeEnabled)
                {
                    Directory.CreateDirectory(ProfilesFolder);
                    if (Directory.Exists(profDir))
                    {
                        if (!Directory.Exists(profDirTemp) && Directory.Exists(profDirMain))
                        {
                            // Get the subdirectories for the specified directory.
                            DirectoryInfo dir = new DirectoryInfo(profDirMain);
                            Directory.CreateDirectory(profDirTemp);
                            // Get the files in the directory and copy them to the new location.
                            FileInfo[] files = dir.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                string tempPath = Path.Combine(profDirTemp, file.Name);
                                file.CopyTo(tempPath, false);
                            }
                        }
                        else if (!Directory.Exists(profDirMain) && Directory.Exists(profDirTemp))
                        {
                            // Get the subdirectories for the specified directory.
                            DirectoryInfo dir = new DirectoryInfo(profDirTemp);
                            Directory.CreateDirectory(profDirMain);
                            // Get the files in the directory and copy them to the new location.
                            FileInfo[] files = dir.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                string tempPath = Path.Combine(profDirMain, file.Name);
                                file.CopyTo(tempPath, false);
                            }
                        }

                        // First generation no longer exists, it will be generated on demand while you edit.
                        Directory.CreateDirectory(profDir);
                        Directory.CreateDirectory(profDirMain);
                        Directory.CreateDirectory(profDirTemp);
                        if (!Directory.Exists(profDir) || !Directory.Exists(profDirMain) || !Directory.Exists(profDirTemp))
                        {
                            MessageBox.Show("Profile should exist, but does not. Insufficient permissions? Profile mode is disabled.");
                            ProfileViewModel.ProfileModeEnabled = false;
                            return;
                        }

                        if (!ProfileViewModel.ProfileMessageShown)
                        {
                            MessageBox.Show(@"The profile for your game loaded successfully!

UndertaleModTool now uses the ""Profile"" system by default for code.
Using the profile system, many new features are available to you!
For example, the code is fully editable (you can even add comments)
and it will be saved exactly as you wrote it. In addition, if the
program crashes or your computer loses power during editing, your
code edits will be recovered automatically the next time you start
the program.

The profile system can be toggled on or off at any time by going
to the ""File"" tab at the top and then opening the ""Settings""
(the ""Enable profile mode"" option toggles it on or off).
You may wish to disable it for purposes such as collaborative
modding projects, or when performing technical operations.
For more in depth information, please read ""About_Profile_Mode.txt"".

It should be noted that this system is somewhat experimental, so
should you encounter any problems, please let us know or leave
an issue on GitHub.");
                            ProfileViewModel.ProfileMessageShown = true;
                        }
                        CreateUMTLastEdited(filename);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task LoadFile(string filename, bool preventClose = false)
        {
            await LoaderDialogFactory.Create(perent, preventClose);
            DisposeGameData();
            Task t = Task.Run(() =>
            {
                bool hadWarnings = false;
                UndertaleData data = null;
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        data = UndertaleIO.Read(stream, warning =>
                        {
                            MessageBox.Show($"Loading warning\n{warning}", perent);
                            hadWarnings = true;
                        }, async message =>
                        {
                            await LoaderDialogFactory.UpdateProgressStatus(message);
                        });
                    }

                    UndertaleEmbeddedTexture.TexData.ClearSharedStream();
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error occured while trying to load:\n" + e.Message);
                }

                Dispatcher.UIThread.InvokeAsync( async () =>
                {
                    if (data != null)
                    {
                        if (data.UnsupportedBytecodeVersion)
                        {
                            MessageBox.Show("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.");
                            CanSave = false;
                            CanSafelySave = false;
                        }
                        else if (hadWarnings)
                        {
                            MessageBox.Show("Warnings occurred during loading. Data loss will likely occur when trying to save!");
                            CanSave = true;
                            CanSafelySave = false;
                        }
                        else
                        {
                            CanSave = true;
                            CanSafelySave = true;
                            await UpdateProfile(data, filename);
                            if (data != null)
                            {
                                data.ToolInfo.ProfileMode = ProfileViewModel.ProfileModeEnabled;
                                data.ToolInfo.CurrentMD5 = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                            }
                        }
                        if (data.IsYYC())
                        {
                            MessageBox.Show("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.");
                        }
                        if (data.GeneralInfo != null)
                        {
                            if (!data.GeneralInfo.IsDebuggerDisabled)
                            {
                                MessageBox.Show("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.");
                            }
                        }
                        if (Path.GetDirectoryName(FilePath) != Path.GetDirectoryName(filename))
                            CloseChildFiles();

                        if (FilePath != filename)
                            await SaveGMLCache(FilePath, false, dialog);

                        Data = data;

                        //                await LoadGMLCache(filename, dialog);
                        //                UndertaleCachedImageLoader.Reset();
                        //                CachedTileDataLoader.Reset();

                        //                Data.ToolInfo.AppDataProfiles = ProfilesFolder;
                        //                FilePath = filename;
                        //                OnPropertyChanged("Data");
                        //                OnPropertyChanged("FilePath");
                        //                OnPropertyChanged("IsGMS2");

                        //                BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                        //                                              ? "Tile sets"
                        //                                              : "Backgrounds & Tile sets";

                        //#pragma warning disable CA1416
                        //                UndertaleCodeEditor.gettext = null;
                        //                UndertaleCodeEditor.gettextJSON = null;
                        //#pragma warning restore CA1416
                    }
                    LoaderDialogFactory.HideProgressBar();
                });
            });
            await t;

            // Clear "GC holes" left in the memory in process of data unserializing
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private async Task SaveFile(string filename, bool suppressDebug = false)
        {

        }
    }
}
