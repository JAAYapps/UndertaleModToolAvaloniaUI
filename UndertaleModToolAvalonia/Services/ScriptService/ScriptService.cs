using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Messages;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Services.ScriptService
{
    public class ScriptService : IScriptService
    {
        private readonly EditorViewModel editorViewModel;
        private readonly IDialogService dialogService;
        private readonly ILoadingDialogService loadingDialogService;
        private readonly IFileService fileService;

        private const string DataFileFilter = "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";

        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;
        private bool finishedMessageEnabled = true;

        private int progressValue;
        private double? progressMaximum;
        private string savedStatusText = "";
        private bool isScriptDialogInitialized;
        private Task? updater;
        private CancellationTokenSource? cts;
        private CancellationToken cToken;

        public UndertaleData Data => AppConstants.Data!;
        public ProjectContext Project => null!;
        public string FilePath => AppConstants.FilePath!;
        public string ScriptPath => AppConstants.ScriptPath!;
        public object Highlighted => editorViewModel.SelectedNode?.Model ?? editorViewModel.Highlighted;
        public object Selected => (editorViewModel.CurrentTab?.CurrentObject as EditorContentViewModel)?.Asset!;
        public bool CanSave => Settings.Instance.CanSave;
        public bool ScriptExecutionSuccess { get; private set; } = true;
        public string ScriptErrorMessage { get; private set; } = "";
        public string ExePath { get; } = Settings.GetExecutableDirectory() ?? string.Empty;
        public string ScriptErrorType { get; private set; } = "";
        public bool IsAppClosed => editorViewModel.LsAppClosed;
        public Action<Action> MainThreadAction => (f) => Dispatcher.UIThread.Invoke(f);

        public ScriptService(EditorViewModel editorViewModel, IDialogService dialogService, ILoadingDialogService loadingDialogService, IFileService fileService)
        {
            this.fileService = fileService;
            this.editorViewModel = editorViewModel;
            this.dialogService = dialogService;
            this.loadingDialogService = loadingDialogService;

            scriptSetupTask = Task.Run(() =>
            {
                scriptOptions = ScriptingUtil.CreateDefaultScriptOptions()
                                             .AddImports("UndertaleModToolAvalonia")
                                             .AddReferences(GetType().GetTypeInfo().Assembly,
                                                            typeof(JsonConvert).GetTypeInfo().Assembly);
            });
        }

        public async Task RunScript(string path)
        {
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
            ScriptErrorType = "";
            InitializeScriptDialog();
            editorViewModel.IsEnabled = false;

            await RunScriptNow(path);
            HideProgressBar();
            isScriptDialogInitialized = false;
            editorViewModel.ReloadTree();
            editorViewModel.IsEnabled = true;
        }

        private async Task RunScriptNow(string path)
        {
            string scriptText = $"#line 1 \"{path}\"\n" + File.ReadAllText(path, Encoding.UTF8);

            SetUMTConsoleText("Running " + Path.GetFileName(path) + " ...");
            try
            {
                if (!scriptSetupTask.IsCompleted)
                {
                    await scriptSetupTask;
                }

                AppConstants.ScriptPath = path;

                object result = await CSharpScript.EvaluateAsync(scriptText, scriptOptions.WithFilePath(path).WithFileEncoding(Encoding.UTF8), this, typeof(IScriptInterface));

                if (finishedMessageEnabled)
                {
                    SetUMTConsoleText(result != null ? result.ToString()! : Path.GetFileName(path) + " finished!");
                }
                else
                {
                    finishedMessageEnabled = true;
                }
            }
            catch (CompilationErrorException exc)
            {
                Console.WriteLine(exc.ToString());
                SetUMTConsoleText(exc.Message);
                WaitFor(() => Application.Current!.ShowError(exc.Message, "Script compile error"));
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "CompilationErrorException";
            }
            catch (Exception exc)
            {
                await StopProgressBarUpdater();

                Console.WriteLine(exc.ToString());
                SetUMTConsoleText(exc.Message);
                WaitFor(() => Application.Current!.ShowError(ScriptingUtil.PrettifyException(in exc), "Script error"));
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "Exception";
            }

            GC.Collect();
        }

        private static T WaitFor<T>(Func<Task<T>> show)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => WaitFor(show));

            DispatcherFrame frame = new();
            Task<T> task = show();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.PushFrame(frame);
            return task.GetAwaiter().GetResult();
        }

        private static ScriptException NotPorted([CallerMemberName] string member = "")
        {
            return new ScriptException(member + " is not ported yet.");
        }

        public void ScriptMessage(string message)
        {
            WaitFor(() => Application.Current!.ShowMessage(message, "Script message"));
        }
        public bool ScriptQuestion(string message)
        {
            ScriptMessages.PlayInformationSound();
            return WaitFor(() => Application.Current!.ShowQuestion(message, Icon.Question, "Script Question")) == ButtonResult.Yes;
        }
        public void ScriptWarning(string message)
        {
            WaitFor(() => Application.Current!.ShowWarning(message, "Script warning"));
        }
        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            WaitFor(() => Application.Current!.ShowError(error, title));
            if (SetConsoleText)
            {
                SetUMTConsoleText(error);
                SetFinishedMessage(false);
            }
        }

        public void SetUMTConsoleText(string message)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                editorViewModel.CommandBoxText = message;
            });
        }
        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                finishedMessageEnabled = isFinishedMessageEnabled;
            });
        }

        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
        {
            if (showDialog)
            {
                return WaitFor(() => dialogService.ShowDialogAsync<TextInputViewModel, TextInputParameters, string>(new TextInputParameters(title, label, defaultValue, allowMultiline)))!;
            }
            else
            {
                _ = dialogService.ShowAsync<TextOutputViewModel, TextOutputParameters>(new TextOutputParameters(title, label, defaultValue, allowMultiline, false));
                return null!;
            }
        }

        public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
        {
            _ = dialogService.ShowAsync<TextOutputViewModel, TextOutputParameters>(new TextOutputParameters(title, label, message, allowMultiline, true));
        }

        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
        {
            string? result;
            do
            {
                result = WaitFor(() => dialogService.ShowDialogAsync<TextInputViewModel, TextInputParameters, string>(new TextInputParameters(title, label, defaultInput, isMultiline, submitText, preventClose ? "" : cancelText)));
            }
            while (preventClose && result is null);

            return result!;
        }

        public void InitializeScriptDialog()
        {
            if (!isScriptDialogInitialized)
            {
                isScriptDialogInitialized = true;
                progressMaximum = null;
                savedStatusText = "";
                _ = loadingDialogService.UpdateStatusAsync("Please wait...");
            }
        }

        private void ShowProgressBar()
        {
            loadingDialogService.Show("Script in progress...", "Please wait...");
            _ = loadingDialogService.SetIndeterminateAsync(progressMaximum is null);
        }

        private void ReportProgress(string status, double value)
        {
            _ = loadingDialogService.UpdateStatusAsync(value + "/" + progressMaximum + (!String.IsNullOrEmpty(status) ? ": " + status : ""));
            if (progressMaximum is double maximum)
                _ = loadingDialogService.UpdateProgressAsync(value, maximum);
        }

        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (!isScriptDialogInitialized)
                return;

            if (maxValue != 0)
                progressMaximum = maxValue;

            ShowProgressBar();

            if (message != null)
                _ = loadingDialogService.UpdateMessageAsync(message);

            ReportProgress(status, progressValue);
        }

        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            if (isScriptDialogInitialized)
            {
                this.progressValue = (int)progressValue;
                savedStatusText = status;

                UpdateProgressBar(message, status, progressValue, maxValue);
            }
        }
        public void SetProgressBar()
        {
            if (isScriptDialogInitialized)
                ShowProgressBar();
        }

        public void UpdateProgressValue(double progressValue)
        {
            if (isScriptDialogInitialized)
                ReportProgress(savedStatusText, progressValue);
        }

        public void UpdateProgressStatus(string status)
        {
            if (isScriptDialogInitialized)
                _ = loadingDialogService.UpdateStatusAsync(status);
        }

        public void HideProgressBar()
        {
            if (isScriptDialogInitialized)
                loadingDialogService.Hide();
        }

        public void AddProgress(int amount)
        {
            progressValue += amount;
        }
        public void IncrementProgress()
        {
            progressValue++;
        }
        public void AddProgressParallel(int amount)
        {
            Interlocked.Add(ref progressValue, amount);
        }
        public void IncrementProgressParallel()
        {
            Interlocked.Increment(ref progressValue);
        }
        public int GetProgress()
        {
            return progressValue;
        }
        public void SetProgress(int value)
        {
            progressValue = value;
        }

        private void ProgressUpdater()
        {
            Stopwatch sw = new();
            Stopwatch? swTimeout = null;
            int prevValue = 0;

            while (true)
            {
                sw.Restart();

                if (cToken.IsCancellationRequested)
                {
                    if (prevValue >= progressValue)
                    {
                        sw.Stop();
                        swTimeout?.Stop();
                        return;
                    }
                    else
                    {
                        if (swTimeout is null)
                            swTimeout = Stopwatch.StartNew();
                        else if (swTimeout.ElapsedMilliseconds >= 500)
                        {
                            sw.Stop();
                            swTimeout.Stop();
                            return;
                        }
                    }
                }

                UpdateProgressValue(progressValue);

                prevValue = progressValue;

                Thread.Sleep((int)Math.Max(0, 33 - sw.ElapsedMilliseconds));
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
        public async Task StopProgressBarUpdater()
        {
            if (cts is null || updater is null) return;

            cts.Cancel();

            if (await Task.Run(() => !updater.Wait(2000)))
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

        private static IStorageProvider GetStorageProvider()
        {
            TopLevel? topLevel = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                topLevel = desktop.MainWindow;
            else if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                topLevel = TopLevel.GetTopLevel(singleView.MainView);

            return topLevel?.StorageProvider ?? throw new ScriptException("The dialog could not be opened.");
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            return WaitFor(async () =>
            {
                var files = await fileService.LoadFilteredFileAsync(GetStorageProvider(), filter ?? DataFileFilter, AppConstants.FilePath ?? "");
                return files.FirstOrDefault()?.TryGetLocalPath();
            })!;
        }

        public string PromptSaveFile(string defaultExt, string filter)
        {
            return WaitFor(async () =>
            {
                var file = await fileService.SaveFilteredFileAsync(GetStorageProvider(), defaultExt ?? "win", filter ?? DataFileFilter, AppConstants.FilePath ?? "");
                return file?.TryGetLocalPath();
            })!;
        }

        public string PromptChooseDirectory()
        {
            return WaitFor(async () =>
            {
                var folders = await fileService.LoadFolderAsync(GetStorageProvider(), AppConstants.FilePath ?? "");
                string? path = folders.FirstOrDefault()?.TryGetLocalPath();
                return path is not null ? path + "/" : null;
            })!;
        }

        public bool MakeNewDataFile()
        {
            if (Data != null)
            {
                if (WaitFor(() => Application.Current!.ShowQuestion("Warning: you currently have a project open.\nAre you sure you want to make a new project?")) != ButtonResult.Yes)
                    return false;
            }
            SetUMTConsoleText("");

            Dispatcher.UIThread.Invoke(() =>
            {
                editorViewModel.NewFile();
            });
            return true;
        }

        public void ScriptOpenURL(string url)
        {
            _ = UndertaleHelper.OpenBrowser(url);
        }

        public bool RunUMTScript(string path)
        {
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            WaitFor(async () =>
            {
                await RunScript(path);
                return true;
            });
            if (!ScriptExecutionSuccess)
                ScriptError("An error of type \"" + ScriptErrorType + "\" occurred. The error is:\n\n" + ScriptErrorMessage, ScriptErrorType);
            return ScriptExecutionSuccess;
        }

        public bool LintUMTScript(string path)
        {
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            scriptSetupTask.Wait();
            string[] errors = CSharpScript.Create(File.ReadAllText(path), scriptOptions, typeof(IScriptInterface)).Compile()
                                          .Where(x => x.Severity == DiagnosticSeverity.Error)
                                          .Select(x => x.ToString())
                                          .ToArray();
            if (errors.Length > 0)
            {
                string message = String.Join(Environment.NewLine, errors);
                ScriptError(message, "Script compile error");
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = message;
                ScriptErrorType = "CompilationErrorException";
                return false;
            }
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
            ScriptErrorType = "";
            return true;
        }

        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null!, IDecompileSettings settings = null!)
        {
            return GetDecompiledText(Data.Code.ByName(codeName), context, settings);
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null!, IDecompileSettings settings = null!)
        {
            if (code.ParentEntry is not null)
                return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", decompile that instead.";

            GlobalDecompileContext globalDecompileContext = context is null ? new(Data) : context;
            try
            {
                return code != null
                    ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, settings ?? Data.ToolInfo.DecompilerSettings).DecompileToString()
                    : "";
            }
            catch (Exception e)
            {
                return "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/";
            }
        }

        public string GetDisassemblyText(UndertaleCode code)
        {
            if (code.ParentEntry is not null)
                return $"; This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", disassemble that instead.";

            try
            {
                return code != null ? code.Disassemble(Data.Variables, Data.CodeLocals?.For(code)) : "";
            }
            catch (Exception e)
            {
                return "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/";
            }
        }
        public string GetDisassemblyText(string codeName)
        {
            return GetDisassemblyText(Data.Code.ByName(codeName));
        }

        public void ChangeSelection(object newSelection, bool inNewTab = false)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                WeakReferenceMessenger.Default.Send(new OpenAssetMessage(newSelection, inNewTab));
            });
        }

        public void EnableUI()
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (!editorViewModel.IsEnabled)
                    editorViewModel.IsEnabled = true;
            });
        }
        public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null!) => throw NotPorted();
        public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null!) => throw NotPorted();
    }
}
