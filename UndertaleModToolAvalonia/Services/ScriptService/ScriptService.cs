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
using System.Text;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Services.ScriptService
{
    public class ScriptService : IScriptService
    {
        private readonly EditorViewModel editorViewModel;

        private ScriptOptions scriptOptions;
        private Task scriptSetupTask;
        private bool finishedMessageEnabled = true;

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

        public ScriptService(EditorViewModel editorViewModel)
        {
            this.editorViewModel = editorViewModel;

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
            editorViewModel.IsEnabled = false;

            await RunScriptNow(path);
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

        public bool MakeNewDataFile() => throw NotPorted();
        public void ScriptOpenURL(string url) => throw NotPorted();
        public bool RunUMTScript(string path) => throw NotPorted();
        public bool LintUMTScript(string path) => throw NotPorted();
        public void InitializeScriptDialog() => throw NotPorted();
        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null!, IDecompileSettings settings = null!) => throw NotPorted();
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null!, IDecompileSettings settings = null!) => throw NotPorted();
        public string GetDisassemblyText(string codeName) => throw NotPorted();
        public string GetDisassemblyText(UndertaleCode code) => throw NotPorted();
        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose) => throw NotPorted();
        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true) => throw NotPorted();
        public void SimpleTextOutput(string title, string label, string message, bool allowMultiline) => throw NotPorted();
        public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null!) => throw NotPorted();
        public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null!) => throw NotPorted();
        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue) => throw NotPorted();
        public void SetProgressBar(string message, string status, double progressValue, double maxValue) => throw NotPorted();
        public void SetProgressBar() => throw NotPorted();
        public void UpdateProgressValue(double progressValue) => throw NotPorted();
        public void UpdateProgressStatus(string status) => throw NotPorted();
        public void AddProgress(int amount) => throw NotPorted();
        public void IncrementProgress() => throw NotPorted();
        public void AddProgressParallel(int amount) => throw NotPorted();
        public void IncrementProgressParallel() => throw NotPorted();
        public int GetProgress() => throw NotPorted();
        public void SetProgress(int value) => throw NotPorted();
        public void HideProgressBar() => throw NotPorted();
        public void EnableUI() => throw NotPorted();
        public void StartProgressBarUpdater() => throw NotPorted();
        public Task StopProgressBarUpdater() => throw NotPorted();
        public void ChangeSelection(object newSelection, bool inNewTab = false) => throw NotPorted();
        public string PromptChooseDirectory() => throw NotPorted();
        public string PromptLoadFile(string defaultExt, string filter) => throw NotPorted();
        public string PromptSaveFile(string defaultExt, string filter) => throw NotPorted();
    }
}
