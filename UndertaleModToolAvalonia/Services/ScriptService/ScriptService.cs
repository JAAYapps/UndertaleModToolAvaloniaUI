using Avalonia;
using Avalonia.Threading;
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
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Services.DialogService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Services.ScriptService
{
    public class ScriptService : IScriptService
    {
        private readonly EditorViewModel editorViewModel;
        private readonly IDialogService dialogService;
        private readonly ILoadingDialogService loadingDialogService;

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
        public object Highlighted => throw NotPorted();
        public object Selected => throw NotPorted();
        public bool CanSave => Settings.Instance.CanSave;
        public bool ScriptExecutionSuccess { get; private set; } = true;
        public string ScriptErrorMessage { get; private set; } = "";
        public string ExePath { get; } = Settings.GetExecutableDirectory() ?? string.Empty;
        public string ScriptErrorType { get; private set; } = "";
        public bool IsAppClosed => editorViewModel.LsAppClosed;
        public Action<Action> MainThreadAction => (f) => Dispatcher.UIThread.Invoke(f);

        public ScriptService(EditorViewModel editorViewModel, IDialogService dialogService, ILoadingDialogService loadingDialogService)
        {
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

        public bool MakeNewDataFile() => throw NotPorted();
        public void ScriptOpenURL(string url) => throw NotPorted();
        public bool RunUMTScript(string path) => throw NotPorted();
        public bool LintUMTScript(string path) => throw NotPorted();
        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null!, IDecompileSettings settings = null!) => throw NotPorted();
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null!, IDecompileSettings settings = null!) => throw NotPorted();
        public string GetDisassemblyText(string codeName) => throw NotPorted();
        public string GetDisassemblyText(UndertaleCode code) => throw NotPorted();
        public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null!) => throw NotPorted();
        public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null!) => throw NotPorted();
        public void EnableUI() => throw NotPorted();
        public void ChangeSelection(object newSelection, bool inNewTab = false) => throw NotPorted();
        public string PromptChooseDirectory() => throw NotPorted();
        public string PromptLoadFile(string defaultExt, string filter) => throw NotPorted();
        public string PromptSaveFile(string defaultExt, string filter) => throw NotPorted();
    }
}
