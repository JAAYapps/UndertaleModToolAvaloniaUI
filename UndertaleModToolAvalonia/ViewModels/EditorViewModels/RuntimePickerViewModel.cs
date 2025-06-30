using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels
{
    public partial class RuntimePickerViewModel : ViewModelBase, IInitializable<RuntimePickerParameters>
    {
        [ObservableProperty]
        public ObservableCollection<RuntimeModel> runtimes = new ObservableCollection<RuntimeModel>();

        [ObservableProperty]
        public RuntimeModel? selected = null;

        public async Task<bool> InitializeAsync(RuntimePickerParameters parameters)
        {
            DiscoverRuntimes(parameters.FilePath, parameters.Data);
            if (Runtimes.Count == 0)
            {
                await App.Current!.ShowError("Unable to find game EXE or any installed Studio runtime", "Run error");
                Selected = null;
                return false;
            }
            else if (Runtimes.Count == 1)
            {
                Selected = Runtimes[0];
            }
            return true;
        }

        public void DiscoverRuntimes(string dataFilePath, UndertaleData data)
        {
            Runtimes.Clear();
            DiscoverGameExe(dataFilePath, data);
            DiscoverGMS2();
            DiscoverGMS1();
        }

        private void DiscoverGameExe(string dataFilePath, UndertaleData data)
        {
            string gameExeName = data?.GeneralInfo?.FileName?.Content!;
            if (gameExeName == null)
                return;

            string gameExePath = Path.Combine(Path.GetDirectoryName(dataFilePath)!, gameExeName + ".exe");
            if (!File.Exists(gameExePath))
                return;

            Runtimes.Add(new RuntimeModel() { Version = "Game EXE", Path = gameExePath });
        }

        private void DiscoverGMS1()
        {
            string studioRunner = Path.Combine(Environment.ExpandEnvironmentVariables(Settings.Instance.GameMakerStudioPath), "Runner.exe");
            if (!File.Exists(studioRunner))
                return;

            string? studioDebugger = Path.Combine(Environment.ExpandEnvironmentVariables(Settings.Instance.GameMakerStudioPath), @"GMDebug\GMDebug.exe");
            if (!File.Exists(studioDebugger))
                studioDebugger = null;

            Runtimes.Add(new RuntimeModel() { Version = "1.4.xxx", Path = studioRunner, DebuggerPath = studioDebugger });
        }

        private void DiscoverGMS2()
        {
            string runtimesPath = Environment.ExpandEnvironmentVariables(Settings.Instance.GameMakerStudio2RuntimesPath);
            if (!Directory.Exists(runtimesPath))
                return;

            Regex runtimePattern = new Regex(@"^runtime-(.*)$");
            foreach (var runtimePath in Directory.EnumerateDirectories(runtimesPath))
            {
                Match m = runtimePattern.Match(Path.GetFileName(runtimePath));
                if (!m.Success)
                    continue;

                string runtimeRunner = Path.Combine(runtimePath, @"windows\Runner.exe");
                string runtimeRunnerX64 = Path.Combine(runtimePath, @"windows\x64\Runner.exe");
                if (Environment.Is64BitOperatingSystem && File.Exists(runtimeRunnerX64))
                    runtimeRunner = runtimeRunnerX64;
                if (!File.Exists(runtimeRunner))
                    continue;

                Runtimes.Add(new RuntimeModel() { Version = m.Groups[1].Value, Path = runtimeRunner });
            }
        }
    }
}
