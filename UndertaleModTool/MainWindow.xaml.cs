﻿#pragma warning disable CA1416 // Validate platform compatibility

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using UndertaleModTool.Windows;
using System.IO.Pipes;
using Ookii.Dialogs.Wpf;

using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Runtime;
using SystemJson = System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        /// Note for those who don't know what is "PropertyChanged.Fody" -
        /// it automatically adds "OnPropertyChanged()" to every property (or modify existing) of the class that implements INotifyPropertyChanged.
        /// It does that on code compilation.

        private Tab _currentTab;

        public UndertaleData Data { get; set; }
        public string FilePath { get; set; }
        public string ScriptPath { get; set; } // For the scripting interface specifically

        public string TitleMain { get; set; }

        public static RoutedUICommand CloseTabCommand = new RoutedUICommand("Close current tab", "CloseTab", typeof(MainWindow));
        public static RoutedUICommand CloseAllTabsCommand = new RoutedUICommand("Close all tabs", "CloseAllTabs", typeof(MainWindow));
        public static RoutedUICommand RestoreClosedTabCommand = new RoutedUICommand("Restore last closed tab", "RestoreClosedTab", typeof(MainWindow));
        public static RoutedUICommand SwitchToNextTabCommand = new RoutedUICommand("Switch to the next tab", "SwitchToNextTab", typeof(MainWindow));
        public static RoutedUICommand SwitchToPrevTabCommand = new RoutedUICommand("Switch to the previous tab", "SwitchToPrevTab", typeof(MainWindow));
        public static RoutedUICommand SearchInCodeCommand = new("Search in code", "SearchInCode", typeof(MainWindow));

        public ObservableCollection<Tab> Tabs { get; set; } = new();
        public Tab CurrentTab
        {
            get => _currentTab;
            set
            {
                _currentTab = value;
                OnPropertyChanged();
                OnPropertyChanged("Selected");
            }
        }
        public int CurrentTabIndex { get; set; } = 0;

        public object Highlighted { get; set; }
        public object Selected
        {
            get => CurrentTab?.CurrentObject;
            set
            {
                OnPropertyChanged();
                OpenInTab(value);
            } 
        }

        public Visibility IsGMS2 => (Data?.GeneralInfo?.Major ?? 0) >= 2 ? Visibility.Visible : Visibility.Collapsed;
        // God this is so ugly, if there's a better way, please, put in a pull request
        public Visibility IsExtProductIDEligible => (((Data?.GeneralInfo?.Major ?? 0) >= 2) || (((Data?.GeneralInfo?.Major ?? 0) == 1) && (((Data?.GeneralInfo?.Build ?? 0) >= 1773) || ((Data?.GeneralInfo?.Build ?? 0) == 1539)))) ? Visibility.Visible : Visibility.Collapsed;

        public List<Tab> ClosedTabsHistory { get; } = new();

        private List<(GMImage, WeakReference<BitmapSource>)> _bitmapSourceLookup { get; } = new();
        private object _bitmapSourceLookupLock = new();

        public bool CanSave { get; set; }
        public bool CanSafelySave = false;
        public bool WasWarnedAboutTempRun = false;
        public bool FinishedMessageEnabled = true;
        public bool ScriptExecutionSuccess { get; set; } = true;
        public bool IsSaving { get; set; }
        public string ScriptErrorMessage { get; set; } = "";
        public string ExePath { get; private set; } = Program.GetExecutableDirectory();
        public string ScriptErrorType { get; set; } = "";

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
        private bool _roomRendererEnabled;

        public bool RoomRendererEnabled
        {
            get => _roomRendererEnabled;
            set
            {
                if (UndertaleRoomRenderer.RoomRendererTemplate is null)
                    UndertaleRoomRenderer.RoomRendererTemplate = (DataTemplate)DataEditor.FindResource("roomRendererTemplate");

                if (value)
                {
                    DataEditor.ContentTemplate = UndertaleRoomRenderer.RoomRendererTemplate;
                    UndertaleCachedImageLoader.ReuseTileBuffer = true;
                }
                else
                {
                    DataEditor.ContentTemplate = null;
                    CurrentTab.CurrentObject = LastOpenedObject;
                    LastOpenedObject = null;
                    UndertaleCachedImageLoader.Reset();
                    CachedTileDataLoader.Reset();
                }

                _roomRendererEnabled = value;
            }
        }
        public object LastOpenedObject { get; set; } // for restoring the object that was opened before room rendering

        public bool IsAppClosed { get; set; }

        private HttpClient httpClient;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public void RaiseOnSelectedChanged()
        {
            OnPropertyChanged("Selected");
        }

        // For delivering messages to LoaderDialogs
        public delegate void FileMessageEventHandler(string message);
        public event FileMessageEventHandler FileMessageEvent;

        private LoaderDialog scriptDialog;

        // Related to profile system and appdata
        public byte[] MD5PreviouslyLoaded = null;
        public byte[] MD5CurrentlyLoaded = null;
        public static string AppDataFolder => Settings.AppDataFolder;
        public static string ProfilesFolder { get; } = Path.Combine(Settings.AppDataFolder, "Profiles");
        public static string CorrectionsFolder { get; } = Path.Combine(Program.GetExecutableDirectory(), "Corrections");
        public string ProfileHash = null;
        public bool CrashedWhileEditing = false;

        // Scripting interface-related
        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;

        // Version info
        public static string Edition = "(Git: " + GitVersion.GetGitVersion().Substring(0, 7) + ")";

        // On debug, build with git versions and provided release version. Otherwise, use the provided release version only.
#if DEBUG || SHOW_COMMIT_HASH
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString() + (Edition != "" ? " - " + Edition : "");
#else
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif

        private static readonly Color darkColor = Color.FromArgb(255, 32, 32, 32);
        private static readonly Color darkLightColor = Color.FromArgb(255, 48, 48, 48);
        private static readonly Color whiteColor = Color.FromArgb(255, 222, 222, 222);
        private static readonly SolidColorBrush grayTextBrush = new(Color.FromArgb(255, 179, 179, 179));
        private static readonly SolidColorBrush inactiveSelectionBrush = new(Color.FromArgb(255, 212, 212, 212));
        private static readonly Dictionary<ResourceKey, object> appDarkStyle = new()
        {
            { SystemColors.WindowTextBrushKey, new SolidColorBrush(whiteColor) },
            { SystemColors.ControlTextBrushKey, new SolidColorBrush(whiteColor) },
            { SystemColors.WindowBrushKey, new SolidColorBrush(darkColor) },
            { SystemColors.ControlBrushKey, new SolidColorBrush(darkLightColor) },
            { SystemColors.ControlLightBrushKey, new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) },
            { SystemColors.MenuTextBrushKey, new SolidColorBrush(whiteColor) },
            { SystemColors.MenuBrushKey, new SolidColorBrush(darkLightColor) },
            { SystemColors.GrayTextBrushKey, new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)) },
            { SystemColors.InactiveSelectionHighlightBrushKey, new SolidColorBrush(Color.FromArgb(255, 112, 112, 112)) }
        };

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Open a data.win file to get started, then double click on the items on the left to view them.");
            OpenInTab(Highlighted);

            TitleMain = "UndertaleModTool by krzys_h v:" + Version;

            CanSave = false;
            CanSafelySave = false;

            scriptSetupTask = Task.Run(() =>
            {
                scriptOptions = ScriptOptions.Default
                                .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                                            "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                                            "UndertaleModTool", "System", "System.IO", "System.Collections.Generic",
                                            "System.Text.RegularExpressions")
                                .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                                                GetType().GetTypeInfo().Assembly,
                                                typeof(JsonConvert).GetTypeInfo().Assembly,
                                                typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly,
                                                typeof(ImageMagick.MagickImage).GetTypeInfo().Assembly,
                                                typeof(Underanalyzer.Decompiler.DecompileContext).Assembly)
                                .WithEmitDebugInformation(true); //when script throws an exception, add a exception location (line number)
            });

            var resources = Application.Current.Resources;
            resources["CustomTextBrush"] = SystemColors.ControlTextBrush;
            resources[SystemColors.GrayTextBrushKey] = grayTextBrush;
            resources[SystemColors.InactiveSelectionHighlightBrushKey] = inactiveSelectionBrush;
        }

        /// <summary>
        /// Returns a <see cref="BitmapSource"/> instance for the given <see cref="GMImage"/>.
        /// If a previously-created instance has not yet been garbage collected, this will return that instance.
        /// </summary>
        public BitmapSource GetBitmapSourceForImage(GMImage image)
        {
            lock (_bitmapSourceLookupLock)
            {
                // Look through entire list, clearing out old weak references, and potentially finding our desired source
                BitmapSource foundSource = null;
                for (int i = _bitmapSourceLookup.Count - 1; i >= 0; i--)
                {
                    (GMImage imageKey, WeakReference<BitmapSource> referenceVal) = _bitmapSourceLookup[i];
                    if (!referenceVal.TryGetTarget(out BitmapSource source))
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
                BitmapSource bitmap = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null, pixelData, image.Width * 4);
                bitmap.Freeze();
                _bitmapSourceLookup.Add((image, new WeakReference<BitmapSource>(bitmap)));
                return bitmap;
            }
        }

        private void SetIDString(string str)
        {
            ((Label)this.FindName("ObjectLabel")).Content = str;
        }

        // "attr" is actually "DwmWindowAttribute", but I only need the one value from it
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, int attrSize);
        private const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public void UpdateTree()
        {
            foreach (var child in (MainTree.Items[0] as TreeViewItem).Items)
                ((child as TreeViewItem).ItemsSource as ICollectionView)?.Refresh();
        }
        /*
        private static bool IsLikelyRunFromZipFolder()
        {
            var path = System.Environment.CurrentDirectory;
            var fileInfo = new FileInfo(path);
            return fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
        }

        private static bool IsRunFromTempFolder()
        {
            var path = System.Environment.CurrentDirectory;
            var temp = Path.GetTempPath();
            return path.IndexOf(temp, StringComparison.OrdinalIgnoreCase) == 0;
        }
        */
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // This event is used because on initialization the window handle is null,
            // and on "Window_Loaded" the dark mode for title bar is rendered incorrectly.

            if (!IsVisible || IsLoaded)
                return;

            Settings.Load();

            if (Settings.Instance.RememberWindowPlacements)
                this.SetPlacement(Settings.Instance.MainWindowPlacement);

            if (Settings.Instance.EnableDarkMode)
            {
                SetDarkMode(true, true);
                SetDarkTitleBarForWindow(this, true, false);
            }

            try
            {
                SetTransparencyGridColors(Settings.Instance.TransparencyGridColor1, Settings.Instance.TransparencyGridColor2);
            }
            catch (FormatException) { }
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Load settings to see if we should associate files
                    if (Settings.Instance is null)
                    {
                        Settings.Load();
                    }
                    bool shouldAssociate = true;
                    if (File.Exists("dna.txt"))
                    {
                        shouldAssociate = false;
                    }
                    if (shouldAssociate && Settings.ShouldPromptForAssociations)
                    {
                        if (this.ShowQuestion("First-time setup: Would you like to associate GameMaker data files (like .win) with UndertaleModTool?", MessageBoxImage.Question, "File associations") != MessageBoxResult.Yes)
                        {
                            shouldAssociate = false;
                        }
                    }
                    if (!shouldAssociate)
                    {
                        // Shouldn't associate, so turn it off and save that setting
                        Settings.Instance.AutomaticFileAssociation = false;
                        Settings.Save();
                    }

                    // Associate file types if enabled
                    if (Settings.Instance.AutomaticFileAssociation)
                    {
                        FileAssociations.CreateAssociations();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            var args = Environment.GetCommandLineArgs();
            bool isLaunch = false;
            bool isSpecialLaunch = false;
            if (args.Length > 1)
            {
                if (args.Length > 2)
                {
                    isLaunch = args[2] == "launch";
                    isSpecialLaunch = args[2] == "special_launch";
                }

                string arg = args[1];
                if (File.Exists(arg))
                {
                    await LoadFile(arg, true, isLaunch || isSpecialLaunch);
                }
                else if (arg == "deleteTempFolder") // if was launched from UndertaleModToolUpdater
                {
                    _ = Task.Run(() =>
                    {
                        Process[] updaterInstances = Process.GetProcessesByName("UndertaleModToolUpdater");
                        bool updaterClosed = false;

                        if (updaterInstances.Length > 0)
                        {
                            foreach (Process instance in updaterInstances)
                            {
                                if (!instance.WaitForExit(5000))
                                    this.ShowWarning("UndertaleModToolUpdater app didn't exit.\nCan't delete its temp folder.");
                                else
                                    updaterClosed = true;
                            }
                        }
                        else
                            updaterClosed = true;

                        if (updaterClosed)
                        {
                            bool deleted = false;
                            string exMessage = "(error message is missing)";
                            string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");

                            for (int i = 0; i <= 5; i++)
                            {
                                try
                                {
                                    Directory.Delete(tempFolder, true);

                                    deleted = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    exMessage = ex.Message;
                                }

                                Thread.Sleep(1000);
                            }

                            if (!deleted)
                                this.ShowWarning($"The updater temp folder can't be deleted.\nError - {exMessage}.");
                        }
                    });
                }

                if (isSpecialLaunch)
                {
                    RuntimePicker picker = new RuntimePicker();
                    picker.Owner = this;
                    var runtime = picker.Pick(FilePath, Data);
                    if (runtime == null)
                        return;
                    Process.Start(runtime.Path, "-game \"" + FilePath + "\"");
                    Environment.Exit(0);
                }
                else if (isLaunch)
                {
                    string gameExeName = Data?.GeneralInfo?.FileName?.Content;
                    if (gameExeName == null || FilePath == null)
                    {
                        ScriptError("Null game executable name or location");
                        Environment.Exit(0);
                    }
                    string gameExePath = Path.Combine(Path.GetDirectoryName(FilePath), gameExeName + ".exe");
                    if (!File.Exists(gameExePath))
                    {
                        ScriptError("Cannot find game executable path, expected: " + gameExePath);
                        Environment.Exit(0);
                    }
                    if (!File.Exists(FilePath))
                    {
                        ScriptError("Cannot find data file path, expected: " + FilePath);
                        Environment.Exit(0);
                    }
                    if (gameExeName != null)
                        Process.Start(gameExePath, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                    Environment.Exit(0);
                }
                else if (args.Length > 2)
                {
                    _ = ListenChildConnection(args[2]);
                }
            }

            // Copy the known code corrections into the profile, if they don't already exist.
            ApplyCorrections();
            CrashCheck();

            RunGMSDebuggerItem.Visibility = Settings.Instance.ShowDebuggerOption
                                            ? Visibility.Visible : Visibility.Collapsed;
        }

        public Dictionary<string, NamedPipeServerStream> childFiles = new Dictionary<string, NamedPipeServerStream>();

        public void OpenChildFile(string filename, string chunkName, int itemIndex)
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

            string dir = Path.GetDirectoryName(FilePath);
            Process.Start(Environment.ProcessPath, "\"" + Path.Combine(dir, filename) + "\" " + key);

            var server = new NamedPipeServerStream(key);
            server.WaitForConnection();
            childFiles.Add(filename, server);

            StreamWriter writer = new StreamWriter(childFiles[filename]);
            writer.WriteLine(chunkName + ":" + itemIndex);
            writer.Flush();
        }

        public void CloseChildFiles()
        {
            foreach (var pair in childFiles)
            {
                pair.Value.Close();
            }
            childFiles.Clear();
        }

        public async Task ListenChildConnection(string key)
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
                OpenInTab(Data.EmbeddedAudio[Int32.Parse(thingToOpen[1])], false, "Embedded Audio");
                Activate();
            }
        }

        public static void SetDarkMode(bool enable, bool isStartup = false)
        {
            var resources = Application.Current.Resources;

            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.TabController.SetDarkMode(enable);
            
            if (enable)
            {
                foreach (var pair in appDarkStyle)
                    resources[pair.Key] = pair.Value;

                Windows.TextInput.BGColor = System.Drawing.Color.FromArgb(darkColor.R,
                                                                          darkColor.G,
                                                                          darkColor.B);
                Windows.TextInput.TextBoxBGColor = System.Drawing.Color.FromArgb(darkLightColor.R,
                                                                                 darkLightColor.G,
                                                                                 darkLightColor.B);
                Windows.TextInput.TextColor = System.Drawing.Color.FromArgb(whiteColor.R,
                                                                            whiteColor.G,
                                                                            whiteColor.B);
            }
            else
            {
                foreach (ResourceKey key in appDarkStyle.Keys)
                    resources.Remove(key);

                resources[SystemColors.GrayTextBrushKey] = grayTextBrush;
                resources[SystemColors.InactiveSelectionHighlightBrushKey] = inactiveSelectionBrush;

                Windows.TextInput.BGColor = System.Drawing.SystemColors.Window;
                Windows.TextInput.TextBoxBGColor = System.Drawing.SystemColors.ControlLight;
                Windows.TextInput.TextColor = System.Drawing.SystemColors.ControlText;
            }

            if (!isStartup)
                SetDarkTitleBarForWindows(enable);
        }
        private static void SetDarkTitleBarForWindows(bool enable)
        {
            Window activeWindow = null;
            foreach (Window w in Application.Current.Windows)
            {
                if (w.IsActive)
                {
                    activeWindow = w;
                    break;
                }
            }

            foreach (Window w in Application.Current.Windows)
                SetDarkTitleBarForWindow(w, enable);

            activeWindow?.Activate();
        }
        public static void SetDarkTitleBarForWindow(Window w, bool enable, bool isNotLoaded = true)
        {
            try
            {
                int enableValue = enable ? 1 : 0;
                IntPtr handle = new WindowInteropHelper(w).Handle;
                if (handle == IntPtr.Zero)
                    throw new InvalidOperationException("The window handle is null.");

                _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enableValue, sizeof(int));
                if (isNotLoaded)
                    _ = SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002); // SWP_NOSIZE | SWP_NOMOVE
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetDarkTitleBarForWindow() error for window \"{w}\" - {ex.GetType()}: {ex.Message}");
            }
        }
        public static void SetDarkTitleBarForWindow(System.Windows.Forms.Form form, bool enable, bool isNotLoaded = true)
        {
            try
            {
                int enableValue = enable ? 1 : 0;
                if (form.Handle == IntPtr.Zero)
                    throw new InvalidOperationException("The window handle is null.");

                _ = DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enableValue, sizeof(int));
                if (isNotLoaded)
                    _ = SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002); // SWP_NOSIZE | SWP_NOMOVE
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetDarkTitleBarForWindow() error for window \"{form}\" - {ex.GetType()}: {ex.Message}");
            }
        }

        public static void SetTransparencyGridColors(string color1, string color2)
        {
            Application.Current.Resources["TransparencyGridColor1"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(color1));
            Application.Current.Resources["TransparencyGridColor2"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(color2));
        }

        private void Command_New(object sender, ExecutedRoutedEventArgs e)
        {
            MakeNewDataFile();
        }
        public bool MakeNewDataFile()
        {
            if (Data != null)
            {
                if (this.ShowQuestion("Warning: you currently have a project open.\nAre you sure you want to make a new project?") == MessageBoxResult.No)
                    return false;
            }
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = "";
            });

            DisposeGameData();

            FilePath = null;
            Data = UndertaleData.CreateNew();
            Data.ToolInfo.DecompilerSettings = SettingsWindow.DecompilerSettings;
            Data.ToolInfo.InstanceIdPrefix = () => SettingsWindow.InstanceIdPrefix;
            CloseChildFiles();
            OnPropertyChanged("Data");
            OnPropertyChanged("FilePath");
            OnPropertyChanged("IsGMS2");

            BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                                          ? "Tile sets"
                                          : "Backgrounds & Tile sets";

            Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "New file created, have fun making a game out of nothing\nI TOLD YOU to open a data.win, not create a new file! :P");
            OpenInTab(Highlighted);

            CanSave = true;
            CanSafelySave = true;

            return true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            // ignore drop events inside the main window (e.g. resource tree)
            if (sender is MainWindow)
            {
                // try to detect stuff, autoConvert is false because we don't want any conversion.
                if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                {
                    string filepath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    string fileext = Path.GetExtension(filepath);

                    if (fileext == ".csx")
                    {
                        if (this.ShowQuestion($"Run {filepath} as a script?") == MessageBoxResult.Yes)
                            await RunScript(filepath);
                    }
                    else if (FileAssociations.Extensions.Contains(fileext) || fileext == ".dat" /* audiogroup */)
                    {
                        if (this.ShowQuestion($"Open {filepath} as a data file?") == MessageBoxResult.Yes)
                            await LoadFile(filepath, true);
                    }
                    // else, do something?
                }
            }
        }

        public async Task<bool> DoOpenDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";

            if (dlg.ShowDialog(this) == true)
            {
                await LoadFile(dlg.FileName, true);
                return true;
            }
            return false;
        }
        public async Task<bool> DoSaveDialog(bool suppressDebug = false)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
            dlg.FileName = FilePath;

            if (dlg.ShowDialog(this) == true)
            {
                await SaveFile(dlg.FileName, suppressDebug);
                return true;
            }
            return false;
        }

        public async Task<SaveResult> SaveCodeChanges()
        {
            SaveResult result = SaveResult.NotSaved;

            UndertaleCodeEditor codeEditor;
            try
            {
                DependencyObject child = VisualTreeHelper.GetChild(DataEditor, 0);
                var editor = VisualTreeHelper.GetChild(child, 0);
                if (editor is null)
                    return SaveResult.Error;

                codeEditor = editor as UndertaleCodeEditor;
                if (codeEditor is null)
                    return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return SaveResult.Error;
            }
            
            if (codeEditor.DecompiledChanged || codeEditor.DisassemblyChanged)
            {
                IsSaving = true;

                await codeEditor.SaveChanges();
                //"IsSaving" should became false on success

                result = IsSaving ? SaveResult.Error : SaveResult.Saved;
                IsSaving = false;
            }

            return result;
        }

        private void Command_Open(object sender, ExecutedRoutedEventArgs e)
        {
            _ = DoOpenDialog();
        }

        private async void Command_Save(object sender, ExecutedRoutedEventArgs e)
        {
            if (CanSave)
            {
                if (!CanSafelySave)
                    this.ShowWarning("Errors occurred during loading. High chance of data loss! Proceed at your own risk.");

                var result = await SaveCodeChanges();
                if (result == SaveResult.NotSaved)
                    _ = DoSaveDialog();
                else if (result == SaveResult.Error)
                    this.ShowError("The changes in code editor weren't saved due to some error in \"SaveCodeChanges()\".");
            }
        }
        private async void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Data != null)
            {
                e.Cancel = true;

                bool save = false;

                if (SettingsWindow.WarnOnClose)
                {
                    MessageBoxResult result = this.ShowQuestionWithCancel("Save changes before quitting?");

                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }

                    if (result == MessageBoxResult.Yes)
                    {
                        if (scriptDialog is null || (this.ShowQuestion("Script still runs. Save anyway?\nIt can corrupt the data file that you'll save.") == MessageBoxResult.Yes))
                            save = true;
                    }
                }

                if (save)
                {
                    SaveResult saveRes = await SaveCodeChanges();

                    if (saveRes == SaveResult.NotSaved)
                    {
                        _ = DoSaveDialog();
                    }
                    else if (saveRes == SaveResult.Error)
                    {
                        this.ShowError("The changes in code editor weren't saved due to some error in \"SaveCodeChanges()\".");
                        return;
                    }
                }
                else
                {
                    RevertProfile();
                }

                DestroyUMTLastEdited();

                CloseOtherWindows();

                IsAppClosed = true;

                Closing -= DataWindow_Closing; // Disable "on window closed" event handler (prevent recursion)
                _ = Task.Run(() => Dispatcher.Invoke(Close));
            }

            Settings.Instance.MainWindowPlacement = null;
            if (Settings.Instance.RememberWindowPlacements)
                Settings.Instance.MainWindowPlacement = this.GetPlacement();

            Settings.Save();
        }
        private void Command_Close(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
        private void CloseOtherWindows() //close "standalone" windows (e.g. "ClickableTextOutput")
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is not MainWindow && w.Owner is null) //&& is not a modal window
                    w.Close();
            }
        }

        private void Command_CloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            CloseTab();
        }
        private void Command_CloseAllTabs(object sender, ExecutedRoutedEventArgs e)
        {
            if (Tabs.Count == 1 && CurrentTab.TabTitle == "Welcome!")
                return;

            ClosedTabsHistory.Clear();
            Tabs.Clear();
            CurrentTab = null;

            OpenInTab(new DescriptionView("Welcome to UndertaleModTool!",
                                          "Open data.win file to get started, then double click on the items on the left to view them"));
            CurrentTab = Tabs[CurrentTabIndex];

            UpdateObjectLabel(CurrentTab.CurrentObject);
        }
        private void Command_RestoreClosedTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (ClosedTabsHistory.Count > 0)
            {
                Tab lastTab = ClosedTabsHistory.Last();
                ClosedTabsHistory.RemoveAt(ClosedTabsHistory.Count - 1);

                if (CurrentTab.AutoClose)
                    CloseTab(false);

                Tabs.Insert(lastTab.TabIndex, lastTab);
                CurrentTabIndex = lastTab.TabIndex;

                for (int i = CurrentTabIndex + 1; i < Tabs.Count; i++)
                    Tabs[i].TabIndex = i;

                ScrollToTab(CurrentTabIndex);

                UpdateObjectLabel(lastTab.CurrentObject);
            }
        }
        private void Command_SwitchToNextTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentTabIndex < Tabs.Count - 1)
                CurrentTabIndex++;
        }
        private void Command_SwitchToPrevTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentTabIndex > 0)
                CurrentTabIndex--;
        }
        private void Command_GoBack(object sender, ExecutedRoutedEventArgs e)
        {
            GoBack();
        }
        private void Command_GoForward(object sender, ExecutedRoutedEventArgs e)
        {
            GoForward();
        }

        private void Command_SearchInCode(object sender, ExecutedRoutedEventArgs e)
        {
            string selectedCode = null;
            bool isDisassembly = false;

            var codeEditor = FindVisualChild<UndertaleCodeEditor>(DataEditor);
            if (codeEditor is not null)
            {
                isDisassembly = codeEditor.DisassemblyTab?.IsSelected ?? false;
                if (isDisassembly)
                {
                    selectedCode = codeEditor.DisassemblyEditor?.SelectedText;
                    if (String.IsNullOrEmpty(selectedCode))
                        isDisassembly = false; // Don't check "In assembly" if there is nothing selected in there.
                }
                else
                {
                    selectedCode = codeEditor.DecompiledEditor?.SelectedText;
                }
            }

            SearchInCodeWindow searchInCodeWindow = new(selectedCode, isDisassembly);
            searchInCodeWindow.Show();
        }

        private void DisposeGameData()
        {
            if (Data is not null)
            {
                // This also clears all their game object references
                CurrentTab = null;
                Tabs.Clear();
                ClosedTabsHistory.Clear();

                // Update GUI and wait for all background processes to finish
                UpdateLayout();
                Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                Data.Dispose();
                Data = null;

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
        }
        private async Task LoadFile(string filename, bool preventClose = false, bool onlyGeneralInfo = false)
        {
            LoaderDialog dialog = new LoaderDialog("Loading", "Loading, please wait...");
            dialog.PreventClose = preventClose;
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = "";
            });
            dialog.Owner = this;

            DisposeGameData();
            Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Double click on the items on the left to view them!");
            OpenInTab(Highlighted);

            GameSpecificResolver.BaseDirectory = ExePath;

            Task t = Task.Run(() =>
            {
                bool hadImportantWarnings = false;
                UndertaleData data = null;
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        data = UndertaleIO.Read(stream, (string warning, bool isImportant) =>
                        {
                            this.ShowWarning(warning, "Loading warning");
                            if (isImportant)
                            {
                                hadImportantWarnings = true;
                            }
                        }, message =>
                        {
                            FileMessageEvent?.Invoke(message);
                        }, onlyGeneralInfo);
                    }
                }
                catch (Exception e)
                {
#if DEBUG
                    Debug.WriteLine(e);
#endif
                    this.ShowError("An error occurred while trying to load:\n" + e.Message, "Load error");
                }

                if (onlyGeneralInfo)
                {
                    Dispatcher.Invoke(() =>
                    {
                        dialog.Hide();
                        Data = data;
                        FilePath = filename;
                    });

                    return;
                }

                Dispatcher.Invoke(async () =>
                {
                    if (data != null)
                    {
                        if (data.UnsupportedBytecodeVersion)
                        {
                            this.ShowWarning("Only bytecode versions 13 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version");
                            CanSave = false;
                            CanSafelySave = false;
                        }
                        else if (hadImportantWarnings)
                        {
                            this.ShowWarning("Warnings occurred during loading. Data loss will likely occur when trying to save!", "Loading problems");
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
                                data.ToolInfo.DecompilerSettings = SettingsWindow.DecompilerSettings;
                                data.ToolInfo.InstanceIdPrefix = () => SettingsWindow.InstanceIdPrefix;
                            }
                        }
                        if (data.IsYYC())
                        {
                            this.ShowWarning("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC");
                        }
                        if (data.GeneralInfo != null)
                        {
                            if (!data.GeneralInfo.IsDebuggerDisabled)
                            {
                                this.ShowWarning("This game is set to run with the GameMaker Studio debugger and the normal runtime will simply hang after loading if the debugger is not running. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
                            }
                        }
                        if (Path.GetDirectoryName(FilePath) != Path.GetDirectoryName(filename))
                            CloseChildFiles();

                        Data = data;

                        UndertaleCachedImageLoader.Reset();
                        CachedTileDataLoader.Reset();

                        Data.ToolInfo.DecompilerSettings = SettingsWindow.DecompilerSettings;
                        Data.ToolInfo.InstanceIdPrefix = () => SettingsWindow.InstanceIdPrefix;
                        FilePath = filename;
                        OnPropertyChanged("Data");
                        OnPropertyChanged("FilePath");
                        OnPropertyChanged("IsGMS2");

                        BackgroundsItemsList.Header = IsGMS2 == Visibility.Visible
                                                      ? "Tile sets"
                                                      : "Backgrounds & Tile sets";

                        UndertaleCodeEditor.gettext = null;
                        UndertaleCodeEditor.gettextJSON = null;
                    }

                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;

            // Clear "GC holes" left in the memory in process of data unserializing
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode?view=net-6.0
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private async Task SaveFile(string filename, bool suppressDebug = false)
        {
            if (Data == null || Data.UnsupportedBytecodeVersion)
                return;

            bool isDifferentPath = FilePath != filename;

            LoaderDialog dialog = new LoaderDialog("Saving", "Saving, please wait...");
            dialog.PreventClose = true;
            IProgress<Tuple<int, string>> progress = new Progress<Tuple<int, string>>(i => { dialog.ReportProgress(i.Item2, i.Item1); });
            IProgress<double?> setMax = new Progress<double?>(i => { dialog.Maximum = i; });
            dialog.Owner = this;
            FilePath = filename;
            OnPropertyChanged("FilePath");
            if (Path.GetDirectoryName(FilePath) != Path.GetDirectoryName(filename))
                CloseChildFiles();

            DebugDataDialog.DebugDataMode debugMode = DebugDataDialog.DebugDataMode.NoDebug;
            if (!suppressDebug && Data.GeneralInfo != null && !Data.GeneralInfo.IsDebuggerDisabled)
                this.ShowWarning("You are saving the game in GameMaker Studio debug mode. Unless the debugger is running, the normal runtime will simply hang after loading. You can turn this off in General Info by checking the \"Disable Debugger\" box and saving.", "GMS Debugger");
            Task t = Task.Run(async () =>
            {
                bool SaveSucceeded = true;

                try
                {
                    using (var stream = new FileStream(filename + "temp", FileMode.Create, FileAccess.Write))
                    {
                        UndertaleIO.Write(stream, Data, message =>
                        {
                            FileMessageEvent?.Invoke(message);
                        });
                    }

                    if (debugMode != DebugDataDialog.DebugDataMode.NoDebug)
                    {
                        FileMessageEvent?.Invoke("Generating debugger data...");

                        UndertaleDebugData debugData = UndertaleDebugData.CreateNew();

                        setMax.Report(Data.Code.Count);
                        int count = 0;
                        object countLock = new object();
                        string[] outputs = new string[Data.Code.Count];
                        UndertaleDebugInfo[] outputsOffsets = new UndertaleDebugInfo[Data.Code.Count];
                        GlobalDecompileContext context = new(Data);
                        Parallel.For(0, Data.Code.Count, (i) =>
                        {
                            var code = Data.Code[i];

                            if (debugMode == DebugDataDialog.DebugDataMode.Decompiled)
                            {
                                //Debug.WriteLine("Decompiling " + code.Name.Content);
                                string output;
                                try
                                {
                                    output = new Underanalyzer.Decompiler.DecompileContext(context, code, Data.ToolInfo.DecompilerSettings)
                                        .DecompileToString();
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e.Message);
                                    output = "/*\nEXCEPTION!\n" + e.ToString() + "\n*/";
                                }
                                outputs[i] = output;

                                UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();
                                debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = 0, BytecodeOffset = 0 }); // TODO: generate this too! :D
                                outputsOffsets[i] = debugInfo;
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();

                                uint addr = 0;
                                foreach (var instr in code.Instructions)
                                {
                                    if (debugMode == DebugDataDialog.DebugDataMode.FullAssembler || instr.Kind == UndertaleInstruction.Opcode.Pop || instr.Kind == UndertaleInstruction.Opcode.Popz || instr.Kind == UndertaleInstruction.Opcode.B || instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf || instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                                        debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = (uint)sb.Length, BytecodeOffset = addr * 4 });
                                    instr.ToString(sb, code, addr);
                                    addr += instr.CalculateInstructionSize();
                                    sb.Append('\n');
                                }
                                outputs[i] = sb.ToString();
                                outputsOffsets[i] = debugInfo;
                            }

                            lock (countLock)
                            {
                                progress.Report(new Tuple<int, string>(++count, code.Name.Content));
                            }
                        });
                        setMax.Report(null);

                        for (int i = 0; i < Data.Code.Count; i++)
                        {
                            debugData.SourceCode.Add(new UndertaleScriptSource() { SourceCode = debugData.Strings.MakeString(outputs[i]) });
                            debugData.DebugInfo.Add(outputsOffsets[i]);
                            // FIXME: Probably should write something regardless.
                            if (Data.CodeLocals is not null)
                            {
                                debugData.LocalVars.Add(Data.CodeLocals[i]);
                                if (debugData.Strings.IndexOf(Data.CodeLocals[i].Name) < 0)
                                    debugData.Strings.Add(Data.CodeLocals[i].Name);
                                foreach (var local in Data.CodeLocals[i].Locals)
                                    if (debugData.Strings.IndexOf(local.Name) < 0)
                                        debugData.Strings.Add(local.Name);
                            }
                        }

                        using (UndertaleWriter writer = new UndertaleWriter(new FileStream(Path.ChangeExtension(FilePath, ".yydebug"), FileMode.Create, FileAccess.Write)))
                        {
                            debugData.FORM.Serialize(writer);
                            writer.ThrowIfUnwrittenObjects();
                            writer.Flush();
                        }
                    }
                }
                catch (Exception e)
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.ShowError("An error occurred while trying to save:\n" + e.Message, "Save error");
                    });

                    SaveSucceeded = false;
                }
                // Don't make any changes unless the save succeeds.
                try
                {
                    if (SaveSucceeded)
                    {
                        // It saved successfully!
                        // If we're overwriting a previously existing data file, we're going to overwrite it now.
                        // Then, we're renaming it back to the proper (non-temp) file name.
                        File.Move(filename + "temp", filename, true);

                        // Also make the changes to the profile system.
                        await ProfileSaveEvent(Data, filename);
                    }
                    else
                    {
                        // It failed, but since we made a temp file for saving, no data was overwritten or destroyed (hopefully)
                        // We need to delete the temp file though (if it exists).
                        if (File.Exists(filename + "temp"))
                            File.Delete(filename + "temp");
                        // No profile system changes, since the save failed, like a save was never attempted.
                    }
                }
                catch (Exception exc)
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.ShowError("An error occurred while trying to save:\n" + exc.Message, "Save error");
                    });

                    SaveSucceeded = false;
                }

                UndertaleCodeEditor.gettextJSON = null;

                Dispatcher.Invoke(() =>
                {
                    dialog.Hide();
                });
            });
            dialog.ShowDialog();
            await t;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        public string GenerateMD5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fs = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem)
            {
                string item = (e.NewValue as TreeViewItem).Header?.ToString();

                if (item == "Data")
                {
                    Highlighted = new DescriptionView("Welcome to UndertaleModTool!", Data != null ? "Double click on the items on the left to view them" : "Open data.win file to get started");
                    return;
                }

                if (Data == null)
                {
                    Highlighted = new DescriptionView(item, "Load data.win file first");
                    return;
                }

                Highlighted = item switch
                {
                    "General info" => new GeneralInfoEditor(Data?.GeneralInfo, Data?.Options, Data?.Language),
                    "Global init" => new GlobalInitEditor(Data?.GlobalInitScripts),
                    "Game End scripts" => new GameEndEditor(Data?.GameEndScripts),
                    "Variables" => Data.FORM.Chunks["VARI"],
                    _ => new DescriptionView(item, "Expand the list on the left to edit items"),
                };
            }
            else
            {
                Highlighted = e.NewValue;
            }
        }

        private void MainTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenInTab(Highlighted);
        }
        private void MainTree_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == System.Windows.Input.MouseButton.Middle)
            {
                // Gets the clicked visual element by the mouse position (relative to "MainTree").
                // This is used instead of "VisualTreeHelper.HitTest()" because that ignores the visibility of elements,
                // which led to "ghost" hits on empty space.

                // Updated: why I simply didn't use "e.OriginalSource"?
                DependencyObject obj = MainTree.InputHitTest(e.GetPosition(MainTree)) as DependencyObject;
                if (obj is not TextBlock)
                    return;

                TreeViewItem item = GetNearestParent<TreeViewItem>(obj);
                if (item is null)
                    return;

                item.IsSelected = true;

                if (item.DataContext is not UndertaleResource
                    && (item.Tag as string) != "StandaloneTab")
                    return;

                OpenInTab(Highlighted, true);
            }
        }
        private void MainTree_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OpenInTab(Highlighted);
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDropEffects effects = DragDropEffects.Move | DragDropEffects.Link;

                UndertaleObject draggedItem = Highlighted as UndertaleObject;
                if (draggedItem != null)
                {
                    DataObject data = new DataObject(draggedItem);
                    //data.SetText(draggedItem.ToString());
                    /*if (draggedItem is UndertaleEmbeddedTexture)
                    {
                        UndertaleEmbeddedTexture tex = draggedItem as UndertaleEmbeddedTexture;
                        MemoryStream ms = new MemoryStream(tex.TextureData.TextureBlob);
                        PngBitmapDecoder decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        data.SetImage(decoder.Frames[0]);
                        Debug.WriteLine("PNG data attached");
                        effects |= DragDropEffects.Copy;
                    }*/

                    try
                    {
                        DragDrop.DoDragDrop(MainTree, data, effects);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error on handling \"MainTree\" drag&drop:\n{ex}");
                    }
                }
            }
        }
        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject; // TODO: make this more reliable

            TreeViewItem targetTreeItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as UIElement);
            UndertaleObject targetItem = targetTreeItem.DataContext as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Move) && sourceItem != null && targetItem != null && sourceItem != targetItem && sourceItem.GetType() == targetItem.GetType() ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject;

#if DEBUG
            Debug.WriteLine("Format(s) of dropped TreeViewItem - " + String.Join(", ", e.Data.GetFormats()));
#endif

            TreeViewItem targetTreeItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as UIElement);
            UndertaleObject targetItem = targetTreeItem.DataContext as UndertaleObject;

            e.Effects = (e.AllowedEffects.HasFlag(DragDropEffects.Move) && sourceItem != null && targetItem != null && sourceItem != targetItem &&
                         sourceItem.GetType() == targetItem.GetType() && SettingsWindow.AssetOrderSwappingEnabled)
                            ? DragDropEffects.Move : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Move)
            {
                object source = GetNearestParent<TreeViewItem>(targetTreeItem).ItemsSource;
                IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
                int sourceIndex = list.IndexOf(sourceItem);
                int targetIndex = list.IndexOf(targetItem);
                Debug.Assert(sourceIndex >= 0 && targetIndex >= 0);
                list[sourceIndex] = targetItem;
                list[targetIndex] = sourceItem;
            }
            e.Handled = true;
        }

        public static T VisualUpwardSearch<T>(DependencyObject element) where T : class
        {
            T container = element as T;
            while (container == null && element != null)
            {
                element = VisualTreeHelper.GetParent(element);
                container = element as T;
            }
            return container;
        }
        public static T GetNearestParent<T>(DependencyObject item) where T : class
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        public static childItem FindVisualChild<childItem>(DependencyObject obj, string name = null) where childItem : FrameworkElement
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    if (child.Name == name)
                        return child;
                }
                else
                    return child;
            }

            return null;
        }

        private TreeViewItem GetTreeViewItemFor(UndertaleObject obj)
        {
            foreach (var child in (MainTree.Items[0] as TreeViewItem).Items)
            {
                var twi = (child as TreeViewItem).ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                if (twi != null)
                    return twi;
            }
            return null;
        }

        internal void DeleteItem(UndertaleObject obj)
        {
            TreeViewItem container = GetNearestParent<TreeViewItem>(GetTreeViewItemFor(obj));
            object source = container.ItemsSource;
            IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
            bool isLast = list.IndexOf(obj) == list.Count - 1;
            if (this.ShowQuestion("Delete " + obj + "?" + (!isLast ? "\n\nNote that the code often references objects by ID, so this operation is likely to break stuff because other items will shift up!" : ""), isLast ? MessageBoxImage.Question : MessageBoxImage.Warning, "Confirmation" ) == MessageBoxResult.Yes)
            {
                list.Remove(obj);

                while (CloseTab(obj)) ;
                UpdateTree();

                // remove all tabs with deleted object occurrences from the closed tabs history
                for (int i = 0; i < ClosedTabsHistory.Count; i++)
                {
                    if (ClosedTabsHistory[i].CurrentObject == obj)
                        ClosedTabsHistory.RemoveAt(i);
                }
                // remove consecutive duplicates ( { 1, 1, 2 } -> { 1, 2 } )
                for (int i = 0; i < ClosedTabsHistory.Count - 1; i++)
                {
                    if (ClosedTabsHistory[i] == ClosedTabsHistory[i + 1])
                    {
                        ClosedTabsHistory.RemoveAt(i);
                        i--;
                    }
                }

                // remove all deleted object occurrences from all tab histories
                foreach (Tab tab in Tabs)
                {
                    for (int i = 0; i < tab.History.Count; i++)
                    {
                        if (tab.History[i] == obj)
                        {
                            if (i < tab.HistoryPosition)
                                tab.HistoryPosition--;

                            tab.History.RemoveAt(i);
                        }
                    }

                    // remove consecutive duplicates ( { 1, 1, 2 } -> { 1, 2 } )
                    for (int i = 0; i < tab.History.Count - 1; i++)
                    {
                        if (tab.History[i] == tab.History[i + 1])
                        {
                            if (i < tab.HistoryPosition)
                                tab.HistoryPosition--;

                            tab.History.RemoveAt(i);
                            i--;
                        } 
                    }
                }
            }
        }
        internal void CopyItemName(object obj)
        {
            string name = null;

            if (obj is UndertaleNamedResource namedRes)
                name = namedRes.Name?.Content;
            else if (obj is UndertaleString str && str.Content?.Length > 0)
                name = StringTitleConverter.Instance.Convert(str.Content, null, null, null) as string;

            if (name is not null)
            {
                try
                {
                    Clipboard.SetText(name);
                }
                catch (Exception ex)
                {
                    this.ShowError("Can't copy the item name to clipboard due to this error:\n" +
                                   ex.Message + ".\nYou probably should try again.");
                }
            }
            else
                this.ShowWarning("Item name is null.");
        }

        private void MainTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (Highlighted is UndertaleObject obj)
                    DeleteItem(obj);
            }
        }

        private async void CommandBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                Debug.WriteLine(CommandBox.Text);
                e.Handled = true;
                CommandBox.IsEnabled = false;
                object result;
                try
                {
                    if (!scriptSetupTask.IsCompleted)
                        await scriptSetupTask;

                    ScriptPath = null;

                    result = await CSharpScript.EvaluateAsync(CommandBox.Text, scriptOptions, this, typeof(IScriptInterface));
                }
                catch (CompilationErrorException exc)
                {
                    result = exc.Message;
                    Debug.WriteLine(exc);
                }
                catch (Exception exc)
                {
                    result = exc;
                }
                if (FinishedMessageEnabled)
                {
                    Dispatcher.Invoke(() => CommandBox.Text = result != null ? result.ToString() : "");
                }
                else
                {
                    FinishedMessageEnabled = true;
                }

                GC.Collect();
                CommandBox.IsEnabled = true;
            }
        }

        private void Command_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            // TODO: ugly, but I can't get focus to work properly
            /*var command = FindVisualChild<UndertaleRoomEditor>(DataEditor)?.CommandBindings.OfType<CommandBinding>()
                .FirstOrDefault(cmd => cmd.Command == e.Command);

            if (command != null && command.Command.CanExecute(e.Parameter))
                command.Command.Execute(e.Parameter);*/
            FindVisualChild<UndertaleRoomEditor>(DataEditor)?.Command_Copy(sender, e);
        }

        private void Command_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            FindVisualChild<UndertaleRoomEditor>(DataEditor)?.Command_Paste(sender, e);
        }

        private void MainTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private void MenuItem_FindUnreferencedAssets_Click(object sender, RoutedEventArgs e)
        {
            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(Data);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                this.ShowError("An error occurred in the object references related window.\n" +
                               $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }
    
        private void MenuItem_OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            OpenInTab(Highlighted, true);
        }

        private static bool IsValidAssetIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            char firstChar = name[0];
            if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
            {
                return false;
            }
            foreach (char c in name.Skip(1))
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private void MenuItem_Add_Click(object sender, RoutedEventArgs e)
        {
            object source;
            try
            {
                source = (MainTree.SelectedItem as TreeViewItem).ItemsSource;
            }
            catch (Exception ex)
            {
                ScriptError("An error occurred while trying to add the menu item. No action has been taken.\r\n\r\nError:\r\n\r\n" + ex.ToString());
                return;
            }
            IList list = ((source as ICollectionView)?.SourceCollection as IList) ?? (source as IList);
            Type t = list.GetType().GetGenericArguments()[0];
            Debug.Assert(typeof(UndertaleResource).IsAssignableFrom(t));
            UndertaleResource obj = Activator.CreateInstance(t) as UndertaleResource;
            if (obj is UndertaleNamedResource namedResource)
            {
                bool doMakeString = obj is not (UndertaleTexturePageItem or UndertaleEmbeddedAudio or UndertaleEmbeddedTexture);
                string notDataNewName = null;
                if (obj is UndertaleTexturePageItem)
                {
                    notDataNewName = "PageItem " + list.Count;
                }
                if (obj is UndertaleEmbeddedAudio)
                {
                    notDataNewName = "EmbeddedSound " + list.Count;
                }
                if (obj is UndertaleEmbeddedTexture)
                {
                    notDataNewName = "Texture " + list.Count;
                }

                if (doMakeString)
                {
                    string assetTypeName = obj.GetType().Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower();
                    string newName = $"{assetTypeName}{list.Count}";
                    string userNewName = ScriptInputDialog($"Choose new {assetTypeName} name", "Name of new asset:", newName, "Cancel", "Create", false, false);
                    if (userNewName is null)
                    {
                        // Presume user canceled the action
                        return;
                    }
                    if (IsValidAssetIdentifier(userNewName))
                    {
                        newName = userNewName;
                    }
                    else
                    {
                        if (this.ShowQuestionWithCancel($"Asset name \"{userNewName}\" is not a valid identifier. Add a new asset using an auto-generated name instead?",
                            MessageBoxImage.Warning, "Invalid name") != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                    namedResource.Name = Data.Strings.MakeString(newName);
                    if (obj is UndertaleRoom roomResource)
                    {
                        if (Data.IsGameMaker2())
                        {
                            roomResource.Caption = null;
                            roomResource.Backgrounds.Clear();
                            if (Data.IsVersionAtLeast(2024, 13))
                            {
                                roomResource.Flags |= Data.IsVersionAtLeast(2024, 13) ? UndertaleRoom.RoomEntryFlags.IsGM2024_13 : UndertaleRoom.RoomEntryFlags.IsGMS2;
                            }
                            else
                            {
                                roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                                if (Data.IsVersionAtLeast(2, 3))
                                {
                                    roomResource.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
                                }
                            }
                        }
                        else
                        {
                            roomResource.Caption = Data.Strings.MakeString("");
                        }

                        if (this.ShowQuestion("Add the new room to the end of the room order list?", MessageBoxImage.Question, "Add to room order list") == MessageBoxResult.Yes)
                        {
                            Data.GeneralInfo.RoomOrder.Add(new(roomResource));
                        }
                    }
                    else if (obj is UndertaleScript scriptResource)
                    {
                        if (Data.IsVersionAtLeast(2, 3))
                        {
                            scriptResource.Code = UndertaleCode.CreateEmptyEntry(Data, $"gml_GlobalScript_{newName}");
                            if (Data.GlobalInitScripts is IList<UndertaleGlobalInit> globalInitScripts)
                            {
                                globalInitScripts.Add(new UndertaleGlobalInit()
                                {
                                    Code = scriptResource.Code
                                });
                            }
                        }
                        else
                        {
                            scriptResource.Code = UndertaleCode.CreateEmptyEntry(Data, $"gml_Script_{newName}");
                        }
                    }
                    else if (obj is UndertaleCode codeResource)
                    {
                        if (Data.CodeLocals is not null)
                        {
                            codeResource.LocalsCount = 1;
                            UndertaleCodeLocals.CreateEmptyEntry(Data, codeResource.Name);
                        }
                        else
                        {
                            codeResource.WeirdLocalFlag = true;
                        }
                    }
                    else if (obj is UndertaleExtension && IsExtProductIDEligible == Visibility.Visible)
                    {
                        var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
                        Data.FORM.EXTN.productIdData.Add(newProductID);
                    }
                    else if (obj is UndertaleShader shader)
                    {
                        shader.GLSL_ES_Vertex = Data.Strings.MakeString("", true);
                        shader.GLSL_ES_Fragment = Data.Strings.MakeString("", true);
                        shader.GLSL_Vertex = Data.Strings.MakeString("", true);
                        shader.GLSL_Fragment = Data.Strings.MakeString("", true);
                        shader.HLSL9_Vertex = Data.Strings.MakeString("", true);
                        shader.HLSL9_Fragment = Data.Strings.MakeString("", true);
                    }
                }
                else
                {
                    namedResource.Name = new UndertaleString(notDataNewName); // not Data.MakeString!
                }
            }
            else if (obj is UndertaleString str)
            {
                str.Content = "string" + list.Count;
            }
            list.Add(obj);
            UpdateTree();
            HighlightObject(obj);
            OpenInTab(obj, true);
        }

        private void RootMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem_RunScript_SubmenuOpened(sender, e, Path.Combine(ExePath, "Scripts"));
        }

        private void MenuItem_RunScript_SubmenuOpened(object sender, RoutedEventArgs e, string folderDir)
        {
            MenuItem item = sender as MenuItem;

            // DUMB Wpf behaviour. If a child submenu gets triggered, it triggers ALL parent events.
            // So this is needed to prevent triggering parent events.
            e.Handled = true;

            DirectoryInfo directory = new DirectoryInfo(folderDir);
            item.Items.Clear();
            try
            {
                // exit out early if the path does not exist.
                if (!directory.Exists)
                {
                    item.Items.Add(new MenuItem {Header = $"(Path {folderDir} does not exist, cannot search for files!)", IsEnabled = false});

                    if (item.Name == "RootScriptItem")
                    {
                        var otherScripts1 = new MenuItem {Header = "Run _other script..."};
                        otherScripts1.Click += MenuItem_RunOtherScript_Click;
                        item.Items.Add(otherScripts1);
                    }

                    return;
                }

                // Go over each csx file
                foreach (var file in directory.EnumerateFiles("*.csx"))
                {
                    var filename = file.Name;
                    // Replace _ with __ because WPF uses _ for keyboard navigation
                    MenuItem subitem = new MenuItem {Header = filename.Replace("_", "__")};
                    subitem.Click += MenuItem_RunBuiltinScript_Item_Click;
                    subitem.CommandParameter = file.FullName;
                    item.Items.Add(subitem);
                }

                foreach (var subDirectory in directory.EnumerateDirectories())
                {
                    // Don't add directories which don't have script files
                    if (!subDirectory.EnumerateFiles("*.csx").Any())
                        continue;

                    var subDirName = subDirectory.Name;
                    // In addition to the _ comment from above, we also need to add at least one item, so that WPF uses this as a submenuitem
                    MenuItemDark subItem = new() {Header = subDirName.Replace("_", "__"), Items = {new MenuItem {Header = "(loading...)", IsEnabled = false}}};
                    subItem.SubmenuOpened += (o, args) => MenuItem_RunScript_SubmenuOpened(o, args, subDirectory.FullName);
                    item.Items.Add(subItem);
                }

                if (item.Items.Count == 0)
                    item.Items.Add(new MenuItem {Header = "(No scripts found!)", IsEnabled = false});
            }
            catch (Exception err)
            {
                item.Items.Add(new MenuItem {Header = err.ToString(), IsEnabled = false});
            }

            item.UpdateLayout();
            Popup popup = FindVisualChild<Popup>(item);
            var content = popup?.Child as Border;
            if (content is not null)
            {
                if (Settings.Instance.EnableDarkMode)
                    content.Background = appDarkStyle[SystemColors.MenuBrushKey] as SolidColorBrush;
                else
                    content.Background = SystemColors.MenuBrush;
            }

            // If we're at the complete root, we need to add the "Run other script" button as well
            if (item.Name != "RootScriptItem") return;

            var otherScripts = new MenuItem {Header = "Run _other script..."};
            otherScripts.Click += MenuItem_RunOtherScript_Click;
            item.Items.Add(otherScripts);
        }

        private async void MenuItem_RunBuiltinScript_Item_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)(sender as MenuItem).CommandParameter;
            if (!File.Exists(path))
                path = Path.Combine(Program.GetExecutableDirectory(), path);

            if (File.Exists(path))
                await RunScript(path);
            else
                this.ShowError("The script file doesn't exist.");
        }

        private async void MenuItem_RunOtherScript_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "csx";
            dlg.Filter = "Scripts (.csx)|*.csx|All files|*";

            if (dlg.ShowDialog() == true)
            {
                await RunScript(dlg.FileName);
            }
        }

        // Apparently, one null check is not enough for `scriptDialog`
        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            scriptDialog?.Dispatcher.Invoke(DispatcherPriority.Normal, () => {
                scriptDialog?.Update(message, status, progressValue, maxValue);
            });
        }

        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (scriptDialog != null)
            {
                this.progressValue = (int)progressValue;
                scriptDialog.SavedStatusText = status;

                UpdateProgressBar(message, status, progressValue, maxValue);
            }
        }
        public void SetProgressBar()
        {
            if (scriptDialog != null && !scriptDialog.IsVisible)
                scriptDialog.Dispatcher.Invoke(() => {
                    scriptDialog?.Show();
                });
        }

        public void UpdateProgressValue(double progressValue)
        {
            scriptDialog?.Dispatcher.Invoke(DispatcherPriority.Normal, () => {
                scriptDialog?.ReportProgress(progressValue);
            });
        }

        public void UpdateProgressStatus(string status)
        {
            scriptDialog?.Dispatcher.Invoke(DispatcherPriority.Normal, () => {
                scriptDialog?.ReportProgress(status);
            });
        }

        public void HideProgressBar()
        {
            scriptDialog?.TryHide();
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

        public void SyncBinding(string resourceType, bool enable)
        {
            if (resourceType.Contains(',')) //if several types are listed
            {
                string[] resTypes = resourceType.Replace(" ", "").Split(',');

                if (enable)
                {
                    foreach (string resType in resTypes)
                    {
                        IEnumerable resListCollection = Data[resType];
                        if (resListCollection is not null)
                        {
                            BindingOperations.EnableCollectionSynchronization(resListCollection, bindingLock);

                            syncBindings.Add(resType);
                        }
                    }
                }
                else
                {
                    foreach (string resType in resTypes)
                    {
                        if (syncBindings.Contains(resType))
                        {
                            BindingOperations.DisableCollectionSynchronization(Data[resType]);

                            syncBindings.Remove(resType);
                        }
                    }
                }
            }
            else
            {
                if (enable)
                {
                    IEnumerable resListCollection = Data[resourceType];
                    if (resListCollection is not null)
                    {
                        BindingOperations.EnableCollectionSynchronization(resListCollection, bindingLock);

                        syncBindings.Add(resourceType);
                    }
                }
                else if (syncBindings.Contains(resourceType))
                {
                    BindingOperations.DisableCollectionSynchronization(Data[resourceType]);

                    syncBindings.Remove(resourceType);
                }
            }
        }
        public void DisableAllSyncBindings() //disable all sync. bindings
        {
            if (syncBindings.Count <= 0) return;

            foreach (string resType in syncBindings)
                BindingOperations.DisableCollectionSynchronization(Data[resType]);

            syncBindings.Clear();
        }

        private void ProgressUpdater()
        {
            Stopwatch sw = new();
            Stopwatch swTimeout = null;
            int prevValue = 0;

            while (true)
            {
                sw.Restart();

                if (cToken.IsCancellationRequested)
                {
                    if (prevValue >= progressValue) // if reached maximum
                    {
                        sw.Stop();
                        swTimeout?.Stop();
                        return;
                    }
                    else
                    {
                        if (swTimeout is null)
                            swTimeout = Stopwatch.StartNew();          // begin measuring
                        else if (swTimeout.ElapsedMilliseconds >= 500) // timeout - 0.5 seconds
                        {
                            sw.Stop();
                            swTimeout.Stop();
                            return;
                        }
                    }
                }

                UpdateProgressValue(progressValue);

                prevValue = progressValue;

                Thread.Sleep((int)Math.Max(0, 33 - sw.ElapsedMilliseconds)); // ~30 times per second
            }
        }
        public void StartProgressBarUpdater()
        {
            if (cts is not null)
                ScriptWarning("Warning - there is another progress bar updater task running (hangs) in the background.\nRestart the application to prevent some unexpected behavior.");

            cts = new CancellationTokenSource();
            cToken = cts.Token;

            updater = Task.Run(ProgressUpdater);
        }
        public async Task StopProgressBarUpdater() //async because "Wait()" blocks UI thread
        {
            if (cts is null) return;

            cts.Cancel();

            if (await Task.Run(() => !updater.Wait(2000))) //if ProgressUpdater isn't responding
                ScriptError("Stopping the progress bar updater task is failed.\nIt's highly recommended to restart the application.",
                            "Script error", false);
            else
            {
                cts.Dispose();
                cts = null;
            }

            if (!updater.IsCompleted)
                ScriptError("Stopping the progress bar updater task is failed.\nIt's highly recommended to restart the application.",
                            "Script error", false);
            else
                updater.Dispose();
        }

        public void OpenCodeEntry(string name, UndertaleCodeEditor.CodeEditorTab editorTab, bool inNewTab = false)
        {
            OpenCodeEntry(name, -1, editorTab, inNewTab);
        }
        public void OpenCodeEntry(string name, int lineNum, UndertaleCodeEditor.CodeEditorTab editorTab, bool inNewTab = false)
        {
            UndertaleCode code = Data.Code.ByName(name);

            if (code is not null)
            {
                Focus();

                if (Selected == code)
                {
                    var codeEditor = FindVisualChild<UndertaleCodeEditor>(DataEditor);
                    if (codeEditor is null)
                    {
                        Debug.WriteLine("Cannot select the code editor mode tab - the instance is not found.");
                    }
                    else
                    {
                        if (editorTab == UndertaleCodeEditor.CodeEditorTab.Decompiled
                            && !codeEditor.DecompiledTab.IsSelected)
                        {
                            codeEditor.CodeModeTabs.SelectedItem = codeEditor.DecompiledTab;
                        }
                        else if (editorTab == UndertaleCodeEditor.CodeEditorTab.Disassembly
                            && !codeEditor.DisassemblyTab.IsSelected)
                        {
                            codeEditor.CodeModeTabs.SelectedItem = codeEditor.DisassemblyTab;
                        }

                        var editor = editorTab == UndertaleCodeEditor.CodeEditorTab.Decompiled
                                     ? codeEditor.DecompiledEditor : codeEditor.DisassemblyEditor;
                        CurrentTab?.SaveTabContentState();
                        UndertaleCodeEditor.ChangeLineNumber(lineNum, editor);
                    }
                }
                else
                {
                    if (CurrentTab?.CurrentObject is UndertaleCode)
                        CurrentTab.SaveTabContentState();

                    UndertaleCodeEditor.EditorTab = editorTab;
                    UndertaleCodeEditor.ChangeLineNumber(lineNum, editorTab);
                }

                HighlightObject(code);
                ChangeSelection(code, inNewTab);
            }
            else
            {
                this.ShowError($"Can't find code entry \"{name}\".\n(probably, different game data was loaded)");
            }
        }

        public string ProcessException(in Exception exc)
        {
            // Collect all original trace lines that we want to parse
            List<string> traceLines = new();
            Dictionary<string, int> exTypesDict = null;
            if (exc is AggregateException)
            {
                List<string> exTypes = new();

                // Collect trace lines of inner exceptions, and track their exception type names
                foreach (Exception ex in (exc as AggregateException).InnerExceptions)
                {
                    traceLines.AddRange(ex.StackTrace.Split(Environment.NewLine));
                    exTypes.Add(ex.GetType().FullName);
                }

                // Create a mapping of each exception type to the number of its occurrences
                if (exTypes.Count > 1)
                {
                    exTypesDict = exTypes.GroupBy(x => x)
                                         .Select(x => new { Name = x.Key, Count = x.Count() })
                                         .OrderByDescending(x => x.Count)
                                         .ToDictionary(x => x.Name, x => x.Count);
                }
            }
            else if (exc.InnerException is not null)
            {
                // Collect trace lines of single inner exception
                traceLines.AddRange(exc.InnerException.StackTrace.Split(Environment.NewLine));
            }
            traceLines.AddRange(exc.StackTrace.Split(Environment.NewLine));

            // Iterate over all lines in the stack trace, finding their line numbers and file names
            List<(string SourceFile, int LineNum)> loadedScriptLineNums = new();
            int expectedNumScriptTraceLines = 0;
            try
            {
                foreach (string traceLine in traceLines)
                {
                    // Only handle trace lines that come from a script
                    if (traceLine.TrimStart()[..13] == "at Submission") 
                    {
                        // Add to total count of expected script trace lines
                        expectedNumScriptTraceLines++;

                        // Get full path of the script file, within the line
                        string sourceFile = Regex.Match(traceLine, @"(?<=in ).*\.csx(?=:line \d+)").Value;
                        if (!File.Exists(sourceFile))
                            continue;

                        // Try to find line number from the line
                        const string pattern = ":line ";
                        int linePos = traceLine.IndexOf(pattern);
                        if (linePos > 0 && int.TryParse(traceLine[(linePos + pattern.Length)..], out int lineNum))
                        {
                            loadedScriptLineNums.Add((sourceFile, lineNum));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string excString = exc.ToString();

                int endOfPrevStack = excString.IndexOf("--- End of stack trace from previous location ---");
                if (endOfPrevStack != -1)
                {
                    // Keep only stack trace of the script
                    excString = excString[..endOfPrevStack];
                }

                return $"An error occurred while processing the exception text.\nError message - \"{e.Message}\"\nThe unprocessed text is below.\n\n" + excString;
            }

            // Generate final exception text to show.
            // If we found the expected number of script trace lines, then use them; otherwise, use the regular exception text.
            string excText;
            if (loadedScriptLineNums.Count == expectedNumScriptTraceLines)
            {                
                // Read the code for the files to know what the code line associated with the stack trace is
                Dictionary<string, List<string>> scriptsCode = new();
                foreach ((string sourceFile, int _) in loadedScriptLineNums)
                {
                    if (!scriptsCode.ContainsKey(sourceFile))
                    {
                        string scriptCode = null;
                        try
                        {
                            scriptCode = File.ReadAllText(sourceFile, Encoding.UTF8);
                        }
                        catch (Exception e)
                        {
                            string excString = exc.ToString();

                            return $"An error occurred while processing the exception text.\nError message - \"{e.Message}\"\nThe unprocessed text is below.\n\n" + excString;
                        }
                        scriptsCode.Add(sourceFile, scriptCode.Split('\n').ToList());
                    }
                }

                // Generate custom stack trace
                string excLines = string.Join('\n', loadedScriptLineNums.Select(pair =>
                {
                    string scriptName = Path.GetFileName(pair.SourceFile);
                    string scriptLine = scriptsCode[pair.SourceFile][pair.LineNum - 1]; // - 1 because line numbers start from 1
                    return $"Line {pair.LineNum} in script {scriptName}: {scriptLine}"; 
                }));

                if (exTypesDict is not null)
                {
                    string exTypesStr = string.Join(",\n", exTypesDict.Select(x => $"{x.Key}{((x.Value > 1) ? " (x" + x.Value + ")" : string.Empty)}"));
                    excText = $"{exc.GetType().FullName}: One on more errors occurred:\n{exTypesStr}\n\nThe current stacktrace:\n{excLines}";
                }
                else
                {
                    excText = $"{exc.GetType().FullName}: {exc.Message}\n\nThe current stacktrace:\n{excLines}";
                }
            }
            else
            {
                string excString = exc.ToString();

                int endOfPrevStack = excString.IndexOf("--- End of stack trace from previous location ---");
                if (endOfPrevStack != -1)
                {
                    // Keep only stack trace of the script
                    excString = excString[..endOfPrevStack];
                }

                excText = excString;
            }

            return excText;
        }

        public void InitializeProgressDialog(string title, string msg)
        {
            scriptDialog ??= new LoaderDialog(title, msg)
            {
                Owner = this,
                PreventClose = true
            };
        }

        public async Task RunScript(string path)
        {
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
            ScriptErrorType = "";
            InitializeScriptDialog();
            this.IsEnabled = false; // Prevent interaction while the script is running.

            await RunScriptNow(path); // Runs the script now.
            HideProgressBar(); // Hide the progress bar.
            scriptDialog = null;
            this.IsEnabled = true; // Allow interaction again.
        }

        private async Task RunScriptNow(string path)
        {
            string scriptText = $"#line 1 \"{path}\"\n" + File.ReadAllText(path, Encoding.UTF8);

            Dispatcher.Invoke(() => CommandBox.Text = "Running " + Path.GetFileName(path) + " ...");
            try
            {
                if (!scriptSetupTask.IsCompleted)
                    await scriptSetupTask;

                ScriptPath = path;

                object result = await CSharpScript.EvaluateAsync(scriptText, scriptOptions.WithFilePath(path).WithFileEncoding(Encoding.UTF8), this, typeof(IScriptInterface));

                if (FinishedMessageEnabled)
                {
                    Dispatcher.Invoke(() => CommandBox.Text = result != null ? result.ToString() : Path.GetFileName(path) + " finished!");
                }
                else
                {
                    FinishedMessageEnabled = true;
                }
            }
            catch (CompilationErrorException exc)
            {
                Console.WriteLine(exc.ToString());
                Dispatcher.Invoke(() => CommandBox.Text = exc.Message);
                this.ShowError(exc.Message, "Script compile error");
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "CompilationErrorException";
            }
            catch (Exception exc)
            {
                bool isScriptException = exc.GetType().Name == "ScriptException";
                string excString = string.Empty;

                if (!isScriptException)
                    excString = ProcessException(in exc);

                await StopProgressBarUpdater();

                Console.WriteLine(exc.ToString());
                Dispatcher.Invoke(() => CommandBox.Text = exc.Message);
                this.ShowError(isScriptException ? exc.Message : excString, "Script error");
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "Exception";
            }

            GC.Collect();
            scriptText = null;
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = defaultExt ?? "win";
            dlg.Filter = filter ?? "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string PromptSaveFile(string defaultExt, string filter)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = defaultExt ?? "win";
            dlg.Filter = filter ?? "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string PromptChooseDirectory()
        {
            VistaFolderBrowserDialog folderBrowser = new();
            // vista dialog doesn't suffix the folder name with "/", so we're fixing it here.
            return folderBrowser.ShowDialog() == true ? folderBrowser.SelectedPath + "/" : null;
        }

        public void PlayInformationSound()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                System.Media.SystemSounds.Asterisk.Play();
        }

        public void ScriptMessage(string message)
        {
            this.ShowMessage(message, "Script message");
        }
        public bool ScriptQuestion(string message)
        {
            PlayInformationSound();
            return this.ShowQuestion(message, MessageBoxImage.Question, "Script Question") == MessageBoxResult.Yes;
        }
        public void ScriptWarning(string message)
        {
            this.ShowWarning(message, "Script warning");
        }
        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            this.ShowError(error, title);
            if (SetConsoleText)
            {
                SetUMTConsoleText(error);
                SetFinishedMessage(false);
            }
        }

        public void SetUMTConsoleText(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = message;
            });
        }
        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            this.Dispatcher.Invoke(() =>
            {
                FinishedMessageEnabled = isFinishedMessageEnabled;
            });
        }

        public string SimpleTextInput(string titleText, string labelText, string defaultInputBoxText, bool isMultiline, bool showDialog = true)
        {
            TextInput input = new TextInput(labelText, titleText, defaultInputBoxText, isMultiline);

            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
            if (showDialog)
            {
                result = input.ShowDialog();
                input.Dispose();

                if (result == System.Windows.Forms.DialogResult.OK)
                    return input.ReturnString.Replace('\v', '\n'); //values preserved after close; Shift+Enter -> '\v'
                else
                    return null;
            }
            else //if we don't need to wait for result
            {
                input.Show();
                return null;
                //no need to call input.Dispose(), because if form wasn't shown modally, Form.Close() (or closing it with "X") also calls Dispose()
            }
        }

        public void SimpleTextOutput(string titleText, string labelText, string message, bool isMultiline)
        {
            TextInput textOutput = new TextInput(labelText, titleText, message, isMultiline, true); //read-only mode
            textOutput.Show();
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null)
        {
            await Task.Delay(150); //wait until progress bar status is displayed

            ClickableTextOutput textOutput = new(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);

            await textOutput.Dispatcher.InvokeAsync(textOutput.GenerateResults);
            _ = Task.Factory.StartNew(textOutput.FillingNotifier, TaskCreationOptions.LongRunning); //"LongRunning" = prefer creating a new thread

            textOutput.Show();

            PlayInformationSound();
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null)
        {
            await Task.Delay(150);

            ClickableTextOutput textOutput = new(title, query, resultsCount, resultsDict, showInDecompiledView, failedList);

            await textOutput.Dispatcher.InvokeAsync(textOutput.GenerateResults);
            _ = Task.Factory.StartNew(textOutput.FillingNotifier, TaskCreationOptions.LongRunning);

            textOutput.Show();

            PlayInformationSound();
        }

        public void ScriptOpenURL(string url)
        {
            OpenBrowser(url);
        }

        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
        {
            TextInputDialog dlg = new(title, label, defaultInput, cancelText, submitText, isMultiline, preventClose);
            dlg.Owner = this;

            bool? dlgResult = dlg.ShowDialog();
            if (!dlgResult.HasValue || dlgResult == false)
            {
                // returns null (not an empty!!!) string if the dialog has been closed, or an error has occurred.
                return null;
            }

            // otherwise just return the input (it may be empty aka .Length == 0).
            return dlg.InputText;
        }

        private void MenuItem_GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/UnderminersTeam/UndertaleModTool");
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            this.ShowMessage("UndertaleModTool by krzys_h and the Underminers team\nVersion " + Version, "About");
        }

        /// From https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Dialogs/AboutAvaloniaDialog.xaml.cs
        public static void OpenBrowser(string url)
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
                Application.Current.MainWindow.ShowError("Failed to open browser!\n" + e);
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
                Application.Current.MainWindow.ShowError("Failed to open folder!\n" + e);
            }
        }


        private async Task<HttpResponseMessage> HttpGetAsync(string uri)
        {
            try
            {
                return await httpClient.GetAsync(uri);
            }
            catch (Exception exp) when (exp is not NullReferenceException)
            {
                return null;
            }
        }
        public async void UpdateApp(SettingsWindow window)
        {
            //TODO: rewrite this slightly + comment this out so this is clearer on what this does.

            window.UpdateButtonEnabled = false;

            httpClient = new();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            // remove the invalid characters (everything within square brackets) from the version string.
            Regex invalidChars = new Regex(@"Git:|[ (),/:;<=>?@[\]{}]");
            string version = invalidChars.Replace(Version, "");
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UndertaleModTool", version));

            double bytesToMB = 1024 * 1024;

            if (!Environment.Is64BitOperatingSystem)
            {
                this.ShowWarning("Your operating system is 32-bit.\n" +
                                  "The 32-bit (x86) version of UndertaleModTool is obsolete.\n" +
                                  "If you wish to continue using the 32-bit version of UndertaleModTool, either use the GitHub Actions Artifacts, " +
                                  "the Nightly builds if you don't have a GitHub account, or compile UTMT yourself.\n" +
                                  "For any questions or more information, ask in the Underminers Discord server.");
                window.UpdateButtonEnabled = true;
                    return;

            }

            string sysDriveLetter = Path.GetTempPath()[0].ToString();
            if ((new DriveInfo(sysDriveLetter).AvailableFreeSpace / bytesToMB) < 500)
            {
                this.ShowError($"Not enough space on the system drive {sysDriveLetter} - at least 500 MB is required.");
                window.UpdateButtonEnabled = true;
                return;
            }

            string configStr = Version.Contains("Git:") ? "Debug" : "Release";
            bool isSingleFile = !File.Exists(Path.Combine(ExePath, "UndertaleModTool.dll"));
            string assemblyLocation = AppDomain.CurrentDomain.GetAssemblies()
                                      .First(x => x.GetName().Name.StartsWith("System.Collections")).Location; // any of currently used assemblies
            bool isBundled = !Regex.Match(assemblyLocation, @"C:\\Program Files( \(x86\))*\\dotnet\\shared\\").Success;
            string patchName = $"GUI-windows-latest-{configStr}-isBundled-{isBundled.ToString().ToLower()}-isSingleFile-{isSingleFile.ToString().ToLower()}";

            string baseUrl = "https://api.github.com/repos/UnderminersTeam/UndertaleModTool/actions/";
            string detectedActionName = "Publish continuous release of UndertaleModTool";

            // Fetch the latest workflow run
            var result = await HttpGetAsync(baseUrl + "runs?branch=master&status=success&per_page=20");
            if (result?.IsSuccessStatusCode != true)
            {
                string errText = $"{(result is null ? "Check your internet connection." : $"HTTP error - {result.ReasonPhrase}.")}";
                this.ShowError($"Failed to fetch latest build!\n{errText}");
                window.UpdateButtonEnabled = true;
                return;
            }
            // Parse it as JSON
            var actionInfo = JObject.Parse(await result.Content.ReadAsStringAsync());
            var actionList = (JArray)actionInfo["workflow_runs"];
            JObject action = null;

            for (int index = 0; index < actionList.Count; index++)
            {
                var currentAction = (JObject)actionList[index];
                if (currentAction["name"].ToString() == detectedActionName)
                {
                    action = currentAction;
                    break;
                }
            }
            if (action == null)
            {
                this.ShowError($"Failed to find latest build!\nDetected action name - {detectedActionName}");
                window.UpdateButtonEnabled = true;
                return;
            }

            DateTime currDate = File.GetLastWriteTime(Path.Combine(ExePath, "UndertaleModTool.exe"));
            DateTime lastDate = (DateTime)action["updated_at"];
            if (lastDate.Subtract(currDate).TotalMinutes <= 10)
                if (this.ShowQuestion("UndertaleModTool is already up to date.\nUpdate anyway?") != MessageBoxResult.Yes)
                {
                    window.UpdateButtonEnabled = true;
                    return;
                }

            var result2 = await HttpGetAsync($"{baseUrl}runs/{action["id"]}/artifacts"); // Grab information about the artifacts
            if (result2?.IsSuccessStatusCode != true)
            {
                string errText = $"{(result2 is null ? "Check your internet connection." : $"HTTP error - {result2.ReasonPhrase}.")}";
                this.ShowError($"Failed to fetch latest build!\n{errText}");
                window.UpdateButtonEnabled = true;
                return;
            }

            var artifactInfo = JObject.Parse(await result2.Content.ReadAsStringAsync()); // And now parse them as JSON
            var artifactList = (JArray) artifactInfo["artifacts"];                       // Grab the array of artifacts

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                if (this.ShowQuestion("Detected 32-bit (x86) version of UndertaleModTool on an 64-bit operating system.\n" +
                                 "It's highly recommended to use the 64-bit version instead.\n" +
                                 "Do you wish to download it?") != MessageBoxResult.Yes)
                {
                    window.UpdateButtonEnabled = true;
                    return;
                }
            }

            JObject artifact = null;
            for (int index = 0; index < artifactList.Count; index++)
            {
                var currentArtifact = (JObject) artifactList[index];
                string artifactName = (string)currentArtifact["name"];

                // If the tool ever becomes cross platform this needs to check the OS
                if (artifactName.Equals(patchName))
                    artifact = currentArtifact;
            }
            if (artifact is null)
            {
                this.ShowError("Failed to find the artifact!");
                window.UpdateButtonEnabled = true;
                return;
            }

            // GitHub doesn't let anonymous users download artifacts, so let's use nightly.link

            string baseDownloadUrl = artifact["archive_download_url"].ToString();
            string downloadUrl = baseDownloadUrl.Replace("api.github.com/repos", "nightly.link").Replace("/zip", ".zip");

            string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");
            Directory.CreateDirectory(tempFolder); // We're about to download, so make sure the download dir actually exists

            string downloadOutput = Path.Combine(tempFolder, "Update.zip.zip");

            // It's time to download; let's use a cool progress bar
            scriptDialog = new("Downloading", "Downloading new version...")
            {
                PreventClose = true,
                Owner = this,
                StatusText = "Downloaded MB: 0.00"
            };
            SetProgressBar();

            try
            {
                _ = Task.Run(async () =>
                {
                    using (HttpClient httpClient = new() { Timeout = TimeSpan.FromMinutes(5) })
                    {
                        // Read HTTP response
                        using (HttpResponseMessage response = await httpClient.GetAsync(new Uri(downloadUrl), HttpCompletionOption.ResponseHeadersRead))
                        {
                            // Read header
                            response.EnsureSuccessStatusCode();
                            long totalBytes = response.Content.Headers.ContentLength ?? throw new Exception("Missing content length");

                            // Start reading content
                            using Stream contentStream = await response.Content.ReadAsStreamAsync();
                            const int downloadBufferSize = 8192;
                            byte[] downloadBuffer = new byte[downloadBufferSize];

                            // Download content and save to file
                            using FileStream fs = new(downloadOutput, FileMode.Create, FileAccess.Write, FileShare.None, downloadBufferSize, true);
                            int bytesRead = await contentStream.ReadAsync(downloadBuffer);
                            long totalBytesDownloaded = 0;
                            long bytesToUpdateProgress = totalBytes / 500;
                            long bytesToProgressCounter = 0;
                            while (bytesRead > 0)
                            {
                                // Write current data to file
                                await fs.WriteAsync(downloadBuffer.AsMemory(0, bytesRead));

                                // Update progress
                                totalBytesDownloaded += bytesRead;
                                bytesToProgressCounter += bytesRead;
                                if (bytesToProgressCounter >= bytesToUpdateProgress)
                                {
                                    bytesToProgressCounter -= bytesToUpdateProgress;
                                    UpdateProgressStatus($"Downloaded MB: {(totalBytesDownloaded / bytesToMB).ToString("F2", CultureInfo.InvariantCulture)}");
                                }

                                // Read next bytes
                                bytesRead = await contentStream.ReadAsync(downloadBuffer);
                            }
                        }
                    }

                    // Download complete, hide progress bar
                    HideProgressBar();

                    // Extract ZIP
                    string updaterFolderTemp = Path.Combine(tempFolder, "Updater");
                    bool extractedSuccessfully = false;
                    try
                    {
                        // Unzip double-zipped update
                        ZipFile.ExtractToDirectory(downloadOutput, tempFolder, true);
                        File.Move(Path.Combine(tempFolder, $"{patchName}.zip"), Path.Combine(tempFolder, "Update.zip"), true);
                        File.Delete(downloadOutput);

                        string updaterFolder = Path.Combine(ExePath, "Updater");
                        if (!File.Exists(Path.Combine(updaterFolder, "UndertaleModToolUpdater.exe")))
                        {
                            this.ShowError("Updater not found! Aborting update, report this to the devs!\nLocation checked: " + updaterFolder);
                            return;
                        }

                        try
                        {
                            if (Directory.Exists(updaterFolderTemp))
                                Directory.Delete(updaterFolderTemp, true);

                            Directory.CreateDirectory(updaterFolderTemp);
                            foreach (string file in Directory.GetFiles(updaterFolder))
                            {
                                File.Copy(file, Path.Combine(updaterFolderTemp, Path.GetFileName(file)));
                            }
                        }
                        catch (Exception ex)
                        {
                            this.ShowError($"Can't copy the updater app to the temporary folder.\n{ex}");
                            return;
                        }
                        File.WriteAllText(Path.Combine(updaterFolderTemp, "actualAppFolder"), ExePath);

                        extractedSuccessfully = true;
                    }
                    finally
                    {
                        // If we return early or not, always update button status
                        Dispatcher.Invoke(() =>
                        {
                            window.UpdateButtonEnabled = !extractedSuccessfully;
                        });
                    }

                    // Move back to UI thread to perform final actions
                    Dispatcher.Invoke(() =>
                    {
                        this.ShowMessage("UndertaleModTool will now close to finish the update.");

                        // Invoke updater
                        Process.Start(new ProcessStartInfo(Path.Combine(updaterFolderTemp, "UndertaleModToolUpdater.exe"))
                        {
                            WorkingDirectory = updaterFolderTemp
                        });

                        CloseOtherWindows();

                        Closing -= DataWindow_Closing; // disable "on window closed" event handler
                        Close();
                    });
                });
            }
            catch (Exception e)
            {
                string errMsg;
                if (e.InnerException?.InnerException is Exception ex)
                    errMsg = ex.Message;
                else if (e.InnerException is Exception ex1)
                    errMsg = ex1.Message;
                else
                    errMsg = e.Message;

                this.ShowError($"Failed to download new version of UndertaleModTool.\nError - {errMsg}.");
                window.UpdateButtonEnabled = true;
            }
        }

        private async void Command_Run(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
            {
                ScriptError("Nothing to run!");
                return;
            }
            if ((!WasWarnedAboutTempRun) && SettingsWindow.TempRunMessageShow)
            {
                ScriptMessage(@"WARNING:
Temp running the game does not permanently 
save your changes. Please ""Save"" the game
to save your changes. Closing UndertaleModTool
without using the ""Save"" option can
result in loss of work.");
                WasWarnedAboutTempRun = true;
            }
            bool saveOk = true;
            string oldFilePath = FilePath;
            bool oldDisableDebuggerState = true;
            int oldSteamValue = 0;
            oldDisableDebuggerState = Data.GeneralInfo.IsDebuggerDisabled;
            oldSteamValue = Data.GeneralInfo.SteamAppID;
            Data.GeneralInfo.SteamAppID = 0;
            Data.GeneralInfo.IsDebuggerDisabled = true;
            string TempFilesFolder = (oldFilePath != null ? Path.Combine(Path.GetDirectoryName(oldFilePath), "MyMod.temp") : "");
            await SaveFile(TempFilesFolder, false);
            Data.GeneralInfo.SteamAppID = oldSteamValue;
            FilePath = oldFilePath;
            Data.GeneralInfo.IsDebuggerDisabled = oldDisableDebuggerState;
            if (TempFilesFolder == null)
            {
                this.ShowWarning("Temp folder is null.");
                return;
            }
            else if (saveOk)
            {
                string gameExeName = Data?.GeneralInfo?.FileName?.Content;
                if (gameExeName == null || FilePath == null)
                {
                    ScriptError("Null game executable name or location");
                    return;
                }
                string gameExePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), gameExeName + ".exe");
                if (!File.Exists(gameExePath))
                {
                    ScriptError("Cannot find game executable path, expected: " + gameExePath);
                    return;
                }
                if (!File.Exists(TempFilesFolder))
                {
                    ScriptError("Cannot find game path, expected: " + TempFilesFolder);
                    return;
                }
                if (gameExeName != null)
                    Process.Start(gameExePath, "-game \"" + TempFilesFolder + "\" -debugoutput \"" + Path.ChangeExtension(TempFilesFolder, ".gamelog.txt") + "\"");
            }
            else if (!saveOk)
            {
                this.ShowWarning("Temp save failed, cannot run.");
                return;
            }
            if (File.Exists(TempFilesFolder))
            {
                await Task.Delay(3000);
                //File.Delete(TempFilesFolder);
            }
        }
        private async void Command_RunSpecial(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            bool saveOk = true;
            if (!Data.GeneralInfo.IsDebuggerDisabled)
            {
                if (this.ShowQuestion("The game has the debugger enabled. Would you like to disable it so the game will run?") == MessageBoxResult.Yes)
                {
                    Data.GeneralInfo.IsDebuggerDisabled = true;
                    if (!await DoSaveDialog())
                    {
                        this.ShowError("You must save your changes to run.");
                        Data.GeneralInfo.IsDebuggerDisabled = false;
                        return;
                    }
                }
                else
                {
                    this.ShowError("Use the \"Run game using debugger\" option to run this game.");
                    return;
                }
            }
            else
            {
                Data.GeneralInfo.IsDebuggerDisabled = true;
                if (this.ShowQuestion("Save changes first?") == MessageBoxResult.Yes)
                    saveOk = await DoSaveDialog();
            }

            if (FilePath == null)
            {
                this.ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                RuntimePicker picker = new RuntimePicker();
                picker.Owner = this;
                var runtime = picker.Pick(FilePath, Data);
                if (runtime != null)
                    Process.Start(runtime.Path, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
            }
        }

        private async void Command_RunDebug(object sender, ExecutedRoutedEventArgs e)
        {
            if (Data == null)
                return;

            var result = this.ShowQuestion("Are you sure that you want to run the game with GMS debugger?\n" +
                                           "If you want to enable a debug mode in some game, then you need to use one of the scripts.");
            if (result != MessageBoxResult.Yes)
                return;

            bool origDbg = Data.GeneralInfo.IsDebuggerDisabled;
            Data.GeneralInfo.IsDebuggerDisabled = false;

            bool saveOk = await DoSaveDialog(true);
            if (FilePath == null)
            {
                this.ShowWarning("The file must be saved in order to be run.");
            }
            else if (saveOk)
            {
                RuntimePicker picker = new RuntimePicker();
                picker.Owner = this;
                var runtime = picker.Pick(FilePath, Data);
                if (runtime == null)
                    return;
                if (runtime.DebuggerPath == null)
                {
                    this.ShowError("The selected runtime does not support debugging.", "Run error");
                    return;
                }


                string tempProject = Path.GetTempFileName().Replace(".tmp", ".gmx");
                File.WriteAllText(tempProject, @"<!-- Without this file the debugger crashes, but it doesn't actually need to contain anything! -->
<assets>
  <Configs name=""configs"">
    <Config>Configs\Default</Config>
  </Configs>
  <NewExtensions/>
  <sounds name=""sound""/>
  <sprites name=""sprites""/>
  <backgrounds name=""background""/>
  <paths name=""paths""/>
  <objects name=""objects""/>
  <rooms name=""rooms""/>
  <help/>
  <TutorialState>
    <IsTutorial>0</IsTutorial>
    <TutorialName></TutorialName>
    <TutorialPage>0</TutorialPage>
  </TutorialState>
</assets>");

                Process.Start(runtime.Path, "-game \"" + FilePath + "\" -debugoutput \"" + Path.ChangeExtension(FilePath, ".gamelog.txt") + "\"");
                Process.Start(runtime.DebuggerPath, "-d=\"" + Path.ChangeExtension(FilePath, ".yydebug") + "\" -t=\"127.0.0.1\" -tp=" + Data.GeneralInfo.DebuggerPort + " -p=\"" + tempProject + "\"");
            }
            Data.GeneralInfo.IsDebuggerDisabled = origDbg;
        }

        private void Command_Settings(object sender, ExecutedRoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Owner = this;
            settings.ShowDialog();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTree();
        }

        public void UpdateObjectLabel(object obj)
        {
            int foundIndex = obj is UndertaleResource res ? Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            SetIDString(idString);
        }

        public void HighlightObject(object obj, bool silent = true)
        {
            UndertaleResource res = obj as UndertaleResource;
            if (res is null)
            {
                string msg = $"Can't highlight the object - it's null or isn't an UndertaleResource.";
                if (silent)
                    Debug.WriteLine(msg);
                else
                    this.ShowWarning(msg);

                return;
            }

            string objName = null;
            if (obj is not UndertaleNamedResource)
            {
                if (obj is UndertaleVariable var)
                    objName = var.Name?.Content;
            }
            else
                objName = (res as UndertaleNamedResource).Name?.Content;

            ScrollViewer mainTreeViewer = FindVisualChild<ScrollViewer>(MainTree);
            Type objType = res.GetType();

            TreeViewItem resListView = (MainTree.Items[0] as TreeViewItem).Items.Cast<TreeViewItem>()
                                                                                .FirstOrDefault(x => (x.ItemTemplate?.DataType as Type) == objType);
            IList resList;
            try
            {
                resList = Data[res.GetType()];
            }
            catch (Exception ex)
            {
                string msg = $"Can't highlight the object \"{objName}\".\nError - {ex.Message}";
                if (silent)
                    Debug.WriteLine(msg);
                else
                    this.ShowWarning(msg);

                return;
            }

            if (resListView is null)
            {
                string msg = $"Can't highlight the object \"{objName}\" - element with object list not found.";
                if (silent)
                    Debug.WriteLine(msg);
                else
                    this.ShowWarning(msg);

                return;
            }

            double initOffsetV = mainTreeViewer.VerticalOffset;
            double initOffsetH = mainTreeViewer.HorizontalOffset;
            bool initExpanded = resListView.IsExpanded;

            resListView.IsExpanded = true;
            resListView.BringIntoView();
            resListView.UpdateLayout();

            VirtualizingStackPanel resPanel = FindVisualChild<VirtualizingStackPanel>(resListView);
            if (resPanel.Children.Count > 0)
            {
                (resPanel.Children[0] as TreeViewItem).BringIntoView();
                mainTreeViewer.UpdateLayout();

                double firstElemOffset = mainTreeViewer.VerticalOffset + (resPanel.Children[0] as TreeViewItem).TransformToAncestor(mainTreeViewer).Transform(new Point(0, 0)).Y;
                mainTreeViewer.ScrollToVerticalOffset(firstElemOffset + ((resList.IndexOf(res) + 1) * 16) - (mainTreeViewer.ViewportHeight / 2));
            }
            mainTreeViewer.UpdateLayout();

            if (resListView.ItemContainerGenerator.ContainerFromItem(obj) is TreeViewItem resItem)
            {
                Highlighted = resItem.DataContext;
                resItem.IsSelected = true;

                mainTreeViewer.UpdateLayout();
                mainTreeViewer.ScrollToHorizontalOffset(0);
            }
            else
            {
                // revert visual changes
                resListView.IsExpanded = initExpanded;
                resListView.UpdateLayout();
                mainTreeViewer.ScrollToVerticalOffset(initOffsetV);
                mainTreeViewer.ScrollToHorizontalOffset(initOffsetH);
                resListView.UpdateLayout();
            }
        }

        private void GoBack()
        {
            if (CurrentTab.HistoryPosition == 0)
                return;

            CurrentTab.HistoryPosition--;
            CurrentTab.CurrentObject = CurrentTab.History[CurrentTab.HistoryPosition];

            UpdateObjectLabel(CurrentTab.CurrentObject);
        }
        private void GoForward()
        {
            if (CurrentTab.HistoryPosition == CurrentTab.History.Count - 1)
                return;

            CurrentTab.HistoryPosition++;
            CurrentTab.CurrentObject = CurrentTab.History[CurrentTab.HistoryPosition];

            UpdateObjectLabel(CurrentTab.CurrentObject);
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            GoForward();
        }

        public void EnsureDataLoaded()
        {
            if (Data is null)
            {
                throw new ScriptException("No data file is currently loaded!");
            }
        }

        private async void MenuItem_OffsetMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "GameMaker data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*";

            if (dlg.ShowDialog() == true)
            {
                SaveFileDialog dlgout = new SaveFileDialog();

                dlgout.DefaultExt = "txt";
                dlgout.Filter = "Text files (.txt)|*.txt|All files|*";
                dlgout.FileName = dlg.FileName + ".offsetmap.txt";

                if (dlgout.ShowDialog() == true)
                {
                    LoaderDialog dialog = new LoaderDialog("Generating", "Loading, please wait...");
                    dialog.Owner = this;
                    Task t = Task.Run(() =>
                    {
                        try
                        {
                            using (var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
                            {
                                var offsets = UndertaleIO.GenerateOffsetMap(stream);
                                using (var writer = File.CreateText(dlgout.FileName))
                                {
                                    foreach (var off in offsets.OrderBy((x) => x.Key))
                                    {
                                        writer.WriteLine(off.Key.ToString("X8") + " " + off.Value.ToString().Replace("\n", "\\\n"));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this.ShowError("An error occurred while trying to load:\n" + ex.Message, "Load error");
                        }

                        Dispatcher.Invoke(() =>
                        {
                            dialog.Hide();
                        });
                    });
                    dialog.ShowDialog();
                    await t;
                }
            }
        }

        internal void OpenInTab(object obj, bool isNewTab = false, string tabTitle = null)
        {
            if (obj is null)
                return;

            if (obj is DescriptionView && CurrentTab is not null && !CurrentTab.AutoClose)
                return;

            // close auto-closing tab
            if (Tabs.Count > 0 && CurrentTabIndex >= 0 && CurrentTab.AutoClose)
                CloseTab(CurrentTab.TabIndex, false);

            if (isNewTab || Tabs.Count == 0)
            {
                int newIndex = Tabs.Count;
                Tab newTab = new(obj, newIndex, tabTitle);

                Tabs.Add(newTab);
                CurrentTabIndex = newIndex;

                newTab.History.Add(obj);

                if (!TabController.IsLoaded)
                    CurrentTab = newTab;
            }
            else if (obj != CurrentTab?.CurrentObject)
            {
                if (CurrentTab.HistoryPosition < CurrentTab.History.Count - 1)
                {
                    // Remove all objects after the current one (overwrite)
                    int count = CurrentTab.History.Count - CurrentTab.HistoryPosition - 1;
                    for (int i = 0; i < count; i++)
                        CurrentTab.History.RemoveAt(CurrentTab.History.Count - 1);
                }

                CurrentTab.CurrentObject = obj;
                UpdateObjectLabel(obj);

                CurrentTab.History.Add(obj);
                CurrentTab.HistoryPosition++;
            }

            if (DataEditor.IsLoaded)
                GetNearestParent<ScrollViewer>(DataEditor)?.ScrollToTop();
        }

        public void CloseTab(bool addDefaultTab = true) // close the current tab
        {
            CloseTab(CurrentTabIndex, addDefaultTab);
        }
        public void CloseTab(int tabIndex, bool addDefaultTab = true)
        {
            if (tabIndex >= 0 && tabIndex < Tabs.Count)
            {
                Tab closingTab = Tabs[tabIndex];

                TabController.SelectionChanged -= TabController_SelectionChanged;

                int currIndex = CurrentTabIndex;

                // Getting rid of the XAML binding error.
                // See https://stackoverflow.com/a/21001501/12136394
                var item = TabController.ItemContainerGenerator.ContainerFromIndex(tabIndex) as TabItem;
                if (item is not null)
                    item.Template = null;

                // "CurrentTabIndex" changes here (bound to "TabController.SelectedIndex")
                Tabs.RemoveAt(tabIndex);

                if (!closingTab.AutoClose)
                    ClosedTabsHistory.Add(closingTab);

                if (Tabs.Count == 0)
                {
                    if (!closingTab.AutoClose)
                        CurrentTab.SaveTabContentState();

                    CurrentTabIndex = -1;
                    CurrentTab = null;

                    if (addDefaultTab)
                    {
                        OpenInTab(new DescriptionView("Welcome to UndertaleModTool!",
                                                      "Open a data.win file to get started, then double click on the items on the left to view them"));
                        CurrentTab = Tabs[CurrentTabIndex];

                        UpdateObjectLabel(CurrentTab.CurrentObject);
                    }

                    TabController.SelectionChanged += TabController_SelectionChanged;
                }
                else
                {
                    bool tabIsChanged = false;

                    for (int i = tabIndex; i < Tabs.Count; i++)
                        Tabs[i].TabIndex = i;

                    // if closing the currently open tab
                    if (currIndex == tabIndex)
                    {
                        // and if that tab is not the last
                        if (Tabs.Count > 1 && tabIndex < Tabs.Count - 1)
                        {
                            // switch to the last tab
                            currIndex = Tabs.Count - 1;
                        }
                        else
                        {
                            if (currIndex != 0)
                                currIndex -= 1;

                            tabIsChanged = true;
                            CurrentTab.SaveTabContentState();
                        }
                    }
                    else if (currIndex > tabIndex)
                    {
                        currIndex -= 1;
                    }

                    TabController.SelectionChanged += TabController_SelectionChanged;

                    CurrentTabIndex = currIndex;
                    Tab newTab = Tabs[CurrentTabIndex];

                    if (tabIsChanged)
                    {
                        if (closingTab.CurrentObject != newTab.CurrentObject)
                            newTab.PrepareCodeEditor();
                    }

                    CurrentTab = newTab;
                    UpdateObjectLabel(CurrentTab.CurrentObject);

                    if (tabIsChanged)
                        CurrentTab.RestoreTabContentState();
                }
            }
        }
        public bool CloseTab(object obj, bool addDefaultTab = true)
        {
            if (obj is not null)
            {
                int tabIndex = Tabs.FirstOrDefault(x => x.CurrentObject == obj)?.TabIndex ?? -1;
                if (tabIndex != -1)
                {
                    CloseTab(tabIndex, addDefaultTab);
                    return true;
                }
            }
            else
                Debug.WriteLine("Can't close the tab - object is null.");

            return false;
        }

        public void ChangeSelection(object newsel, bool inNewTab = false)
        {
            OpenInTab(newsel, inNewTab);
        }

        private void TabController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabController.SelectedIndex >= 0)
            {
                CurrentTab?.SaveTabContentState();

                Tab newTab = Tabs[CurrentTabIndex];

                if (CurrentTab?.CurrentObject != newTab.CurrentObject)
                    newTab.PrepareCodeEditor();

                CurrentTab = newTab;

                UpdateObjectLabel(CurrentTab.CurrentObject);

                CurrentTab.RestoreTabContentState();

                ScrollToTab(CurrentTabIndex);
            }
        }

        private void ScrollTabs(ScrollDirection dir)
        {
            double offset = TabScrollViewer.HorizontalOffset;
            double clearOffset = 0;
            TabPanel tabPanel = FindVisualChild<TabPanel>(TabController);

            if (Tabs.Count > 1
                && ((dir == ScrollDirection.Left && offset > 0)
                || (dir == ScrollDirection.Right && offset < TabController.ActualWidth)))
            {
                int count = VisualTreeHelper.GetChildrenCount(tabPanel);
                List<TabItem> tabItems = new(count);
                for (int i1 = 0; i1 < count; i1++)
                    tabItems.Add(VisualTreeHelper.GetChild(tabPanel, i1) as TabItem);

                // selected TabItem is in the end of child list somehow, so it should be fixed
                if (CurrentTabIndex != count - 1)
                {
                    tabItems.Insert(CurrentTabIndex, tabItems[^1]);
                    tabItems.RemoveAt(tabItems.Count - 1);
                }

                // get index of first visible tab
                int i = 0;
                foreach (TabItem item in tabItems)
                {
                    double actualWidth = item.ActualWidth;
                    if (i == CurrentTabIndex)
                        actualWidth -= 4; // selected tab is wider

                    clearOffset += actualWidth;

                    if (clearOffset > offset)
                    {
                        if (dir == ScrollDirection.Left)
                            clearOffset -= actualWidth;

                        break;
                    }

                    i++;
                }

                if (dir == ScrollDirection.Left && TabScrollViewer.ScrollableWidth != offset && i != 0)
                    TabScrollViewer.ScrollToHorizontalOffset(clearOffset - tabItems[i - 1].ActualWidth);
                else
                    TabScrollViewer.ScrollToHorizontalOffset(clearOffset);
            }
        }
        private void ScrollToTab(int tabIndex)
        {
            TabScrollViewer.UpdateLayout();

            if (tabIndex == 0)
                TabScrollViewer.ScrollToLeftEnd();
            else if (tabIndex == Tabs.Count - 1)
                TabScrollViewer.ScrollToRightEnd();
            else
            {
                TabPanel tabPanel = FindVisualChild<TabPanel>(TabController);

                int count = VisualTreeHelper.GetChildrenCount(tabPanel);
                List<TabItem> tabItems = new(count);
                for (int i1 = 0; i1 < count; i1++)
                    tabItems.Add(VisualTreeHelper.GetChild(tabPanel, i1) as TabItem);

                // selected TabItem is in the end of child list somehow, so it should be fixed
                if (CurrentTabIndex != count - 1)
                {
                    tabItems.Insert(CurrentTabIndex, tabItems[^1]);
                    tabItems.RemoveAt(tabItems.Count - 1);
                }

                double offset = 0;
                int i = 0;
                foreach (TabItem item in tabItems)
                {
                    if (i == tabIndex)
                        break;

                    offset += item.ActualWidth;
                    i++;
                }

                double endOffset = TabScrollViewer.HorizontalOffset + TabScrollViewer.ViewportWidth;
                if (offset < TabScrollViewer.HorizontalOffset || offset > endOffset)
                    TabScrollViewer.ScrollToHorizontalOffset(offset);
            }
        }
        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollTabs(e.Delta < 0 ? ScrollDirection.Right : ScrollDirection.Left);
            e.Handled = true;
        }
        private void TabScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            initTabContPos = e.GetPosition(TabScrollViewer);
        }
        private void TabsScrollLeftButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollTabs(ScrollDirection.Left);
        }
        private void TabsScrollRightButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollTabs(ScrollDirection.Right);
        }

        private void TabCloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            int tabIndex = (button.DataContext as Tab).TabIndex;

            CloseTab(tabIndex);
        }
        private void TabCloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Button).Content = new Image() { Source = Tab.ClosedHoverIcon };
        }
        private void TabCloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Button).Content = new Image() { Source = Tab.ClosedIcon };
        }

        private void TabItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle)
            {
                TabItem tabItem = sender as TabItem;
                Tab tab = tabItem?.DataContext as Tab;
                if (tab is null)
                    return;

                if (tab.TabTitle != "Welcome!")
                    CloseTab(tab.TabIndex);
            }
        }

        private Point initTabContPos;
        // source - https://stackoverflow.com/a/10738247/12136394
        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source is not TabItemDark tabItem || e.OriginalSource is Button)
                return;

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                // Filter false mouse move events, because it sometimes
                // triggers even on a mouse click
                Point currPos = e.GetPosition(TabScrollViewer);
                if (Math.Abs(Point.Subtract(currPos, initTabContPos).X) < 2)
                    return;

                CurrentTabIndex = tabItem.TabIndex;
                try
                {
                    DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error on handling \"TabItem\" drag&drop:\n{ex}");
                }
            }
        }
        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Source is TabItemDark tabItemTarget &&
                e.Data.GetData(typeof(TabItemDark)) is TabItemDark tabItemSource &&
                !tabItemTarget.Equals(tabItemSource))
            {
                int sourceIndex = tabItemSource.TabIndex;
                int targetIndex = tabItemTarget.TabIndex;
                Tab sourceTab = tabItemSource.DataContext as Tab;
                if (sourceTab is null)
                    return;

                TabController.SelectionChanged -= TabController_SelectionChanged;

                Tabs.RemoveAt(sourceIndex);
                Tabs.Insert(targetIndex, sourceTab);

                for (int i = 0; i < Tabs.Count; i++)
                    Tabs[i].TabIndex = i;

                CurrentTabIndex = targetIndex;

                TabController.SelectionChanged += TabController_SelectionChanged;
            }
        }

        private void CloseTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tab tab = (sender as MenuItem).DataContext as Tab;
            if (tab is null)
                return;

            CloseTab(tab.TabIndex);
        }
        private void CloseOtherTabsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tab tab = (sender as MenuItem).DataContext as Tab;
            if (tab is null)
                return;

            foreach (Tab t in Tabs.Reverse())
            {
                if (t == tab)
                    continue;

                ClosedTabsHistory.Add(t);
            }

            tab.TabIndex = 0;
            Tabs = new() { tab };
            CurrentTabIndex = 0;
        }

        private void TabTitleText_Initialized(object sender, EventArgs e)
        {
            Tab.SetTabTitleBinding(null, null, sender as TextBlock);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;

            // Prevent receiving the mouse wheel event if there is nowhere to scroll.
            if (viewer.ComputedVerticalScrollBarVisibility != Visibility.Visible
                && e.Source == viewer)
                e.Handled = true;
        }

        public bool HasEditorForAsset(object asset)
        {
            if (asset is null)
                return false;

            Type objType = asset.GetType();
            foreach (var key in DataEditor.Resources.Keys)
            {
                if (key is DataTemplateKey templateKey && (templateKey.DataType as Type) == objType)
                    return true;
            }

            return false;
        }
    }

    public class GeneralInfoEditor
    {
        public UndertaleGeneralInfo GeneralInfo { get; private set; }
        public UndertaleOptions Options { get; private set; }
        public UndertaleLanguage Language { get; private set; }

        public GeneralInfoEditor(UndertaleGeneralInfo generalInfo, UndertaleOptions options, UndertaleLanguage language)
        {
            this.GeneralInfo = generalInfo;
            this.Options = options;
            this.Language = language;
        }
    }

    public class GlobalInitEditor
    {
        public IList<UndertaleGlobalInit> GlobalInits { get; private set; }

        public GlobalInitEditor(IList<UndertaleGlobalInit> globalInits)
        {
            this.GlobalInits = globalInits;
        }
    }

    public class GameEndEditor
    {
        public IList<UndertaleGlobalInit> GameEnds { get; private set; }

        public GameEndEditor(IList<UndertaleGlobalInit> GameEnds)
        {
            this.GameEnds = GameEnds;
        }
    }

    public class DescriptionView
    {
        public string Heading { get; private set; }
        public string Description { get; private set; }

        public DescriptionView(string heading, string description)
        {
            Heading = heading;
            Description = description;
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
