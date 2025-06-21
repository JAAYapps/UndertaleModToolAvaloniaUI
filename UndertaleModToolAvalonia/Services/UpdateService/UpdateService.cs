using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.MessageDialogService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Services.UpdateService
{
    public partial class UpdateService : ObservableObject, IUpdateService
    {
        private readonly ILoadingDialogService loadingDialog;
        private static readonly HttpClient httpClient = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUpdateInProgress))]
        private bool isBusy;

        public bool IsUpdateInProgress => IsBusy;

        public UpdateService(ILoadingDialogService loadingDialog)
        {
            this.loadingDialog = loadingDialog;
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

        public async Task CheckForUpdatesAsync()
        {
            //TODO: rewrite this slightly still but for making it check OS for update paths.

            if (IsBusy) return;

            try
            {
                IsBusy = true;
                loadingDialog.Show("Checking for updates...", "Contacting GitHub...");

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                // remove the invalid characters (everything within square brackets) from the version string.
                Regex invalidChars = new Regex(@"Git:|[ (),/:;<=>?@[\]{}]");
                string version = invalidChars.Replace(AppConstants.Version, "");

                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UndertaleModTool", version));

                double bytesToMB = 1024 * 1024;

                if (!Environment.Is64BitOperatingSystem)
                {
                    await App.Current.ShowWarning("Your operating system is 32-bit.\n" +
                                      "The 32-bit (x86) version of UndertaleModTool is obsolete.\n" +
                                      "If you wish to continue using the 32-bit version of UndertaleModTool, either use the GitHub Actions Artifacts, " +
                                      "the Nightly builds if you don't have a GitHub account, or compile UTMT yourself.\n" +
                                      "For any questions or more information, ask in the Underminers Discord server.");
                    IsBusy = false;
                    return;

                }

                string sysDriveLetter = Path.GetTempPath()[0].ToString();
                if ((new DriveInfo(sysDriveLetter).AvailableFreeSpace / bytesToMB) < 500)
                {
                    await App.Current.ShowError($"Not enough space on the system drive {sysDriveLetter} - at least 500 MB is required.");
                    IsBusy = false;
                    return;
                }

                string configStr = AppConstants.Version.Contains("Git:") ? "Debug" : "Release";
                bool isSingleFile = !File.Exists(Path.Combine(AppConstants.LOCATION, "UndertaleModTool.dll"));
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
                    await App.Current.ShowError($"Failed to fetch latest build!\n{errText}");
                    IsBusy = false;
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
                    await App.Current.ShowError($"Failed to find latest build!\nDetected action name - {detectedActionName}");
                    IsBusy = false;
                    return;
                }

                DateTime currDate = File.GetLastWriteTime(Path.Combine(AppConstants.LOCATION, "UndertaleModTool.exe"));
                DateTime lastDate = (DateTime)action["updated_at"];
                if (lastDate.Subtract(currDate).TotalMinutes <= 10)
                    if (await App.Current.ShowQuestion("UndertaleModTool is already up to date.\nUpdate anyway?") != MsBox.Avalonia.Enums.ButtonResult.Yes)
                    {
                        IsBusy = false;
                        return;
                    }

                var result2 = await HttpGetAsync($"{baseUrl}runs/{action["id"]}/artifacts"); // Grab information about the artifacts
                if (result2?.IsSuccessStatusCode != true)
                {
                    string errText = $"{(result2 is null ? "Check your internet connection." : $"HTTP error - {result2.ReasonPhrase}.")}";
                    await App.Current.ShowError($"Failed to fetch latest build!\n{errText}");
                    IsBusy = false;
                    return;
                }

                var artifactInfo = JObject.Parse(await result2.Content.ReadAsStringAsync()); // And now parse them as JSON
                var artifactList = (JArray)artifactInfo["artifacts"];                       // Grab the array of artifacts

                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                    if (await App.Current.ShowQuestion("Detected 32-bit (x86) version of UndertaleModTool on an 64-bit operating system.\n" +
                                     "It's highly recommended to use the 64-bit version instead.\n" +
                                     "Do you wish to download it?") != MsBox.Avalonia.Enums.ButtonResult.Yes)
                    {
                        IsBusy = false;
                        return;
                    }
                }

                JObject artifact = null;
                for (int index = 0; index < artifactList.Count; index++)
                {
                    var currentArtifact = (JObject)artifactList[index];
                    string artifactName = (string)currentArtifact["name"];

                    // If the tool ever becomes cross platform this needs to check the OS
                    // TODO Check the OS that is being used.
                    // Avalonia is cross platform.
                    if (artifactName.Equals(patchName))
                        artifact = currentArtifact;
                }
                if (artifact is null)
                {
                    await App.Current.ShowError("Failed to find the artifact!");
                    IsBusy = false;
                    return;
                }

                // GitHub doesn't let anonymous users download artifacts, so let's use nightly.link

                string baseDownloadUrl = artifact["archive_download_url"].ToString();
                string downloadUrl = baseDownloadUrl.Replace("api.github.com/repos", "nightly.link").Replace("/zip", ".zip");

                string tempFolder = Path.Combine(Path.GetTempPath(), "UndertaleModTool");
                Directory.CreateDirectory(tempFolder); // We're about to download, so make sure the download dir actually exists

                string downloadOutput = Path.Combine(tempFolder, "Update.zip.zip");

                // It's time to download; let's use a cool progress bar from ILoadingDialogService.
                loadingDialog.Show("Downloading", "Downloading new version...");
                await loadingDialog.UpdateStatusAsync("Downloaded MB: 0.00");
                await loadingDialog.SetIndeterminateAsync(false);

                // Use the new service to update the dialog
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

                                    // Update progress using service
                                    totalBytesDownloaded += bytesRead;
                                    bytesToProgressCounter += bytesRead;
                                    if (bytesToProgressCounter >= bytesToUpdateProgress)
                                    {
                                        bytesToProgressCounter -= bytesToUpdateProgress;
                                        await loadingDialog.UpdateProgressAsync((totalBytesDownloaded / bytesToMB), (totalBytes / bytesToMB));
                                        await loadingDialog.UpdateStatusAsync($"Downloaded MB: {(totalBytesDownloaded / bytesToMB).ToString("F2", CultureInfo.InvariantCulture)}");
                                    }

                                    // Read next bytes
                                    bytesRead = await contentStream.ReadAsync(downloadBuffer);
                                }
                            }
                        }

                        // Download complete, hide progress bar
                        loadingDialog.Hide();

                        // Extract ZIP
                        string updaterFolderTemp = Path.Combine(tempFolder, "Updater");
                        bool extractedSuccessfully = false;
                        try
                        {
                            // Unzip double-zipped update
                            ZipFile.ExtractToDirectory(downloadOutput, tempFolder, true);
                            File.Move(Path.Combine(tempFolder, $"{patchName}.zip"), Path.Combine(tempFolder, "Update.zip"), true);
                            File.Delete(downloadOutput);

                            string updaterFolder = Path.Combine(AppConstants.LOCATION, "Updater");
                            if (!File.Exists(Path.Combine(updaterFolder, "UndertaleModToolUpdater.exe")))
                            {
                                await App.Current.ShowError("Updater not found! Aborting update, report this to the devs!\nLocation checked: " + updaterFolder);
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
                                await App.Current.ShowError($"Can't copy the updater app to the temporary folder.\n{ex}");
                                return;
                            }
                            File.WriteAllText(Path.Combine(updaterFolderTemp, "actualAppFolder"), AppConstants.LOCATION);

                            extractedSuccessfully = true;
                        }
                        finally
                        {
                            // If we return early or not, always update button status
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                IsBusy = !extractedSuccessfully;
                            });
                        }

                        // Move back to UI thread to perform final actions
                        await Dispatcher.UIThread.Invoke(async () =>
                        {
                            await App.Current.ShowMessage("UndertaleModTool will now close to finish the update.");

                            // Invoke updater
                            Process.Start(new ProcessStartInfo(Path.Combine(updaterFolderTemp, "UndertaleModToolUpdater.exe"))
                            {
                                WorkingDirectory = updaterFolderTemp
                            });

                            // TODO Must make a closing call system.
                            // CloseOtherWindows();

                            // Closing -= DataWindow_Closing; // disable "on window closed" event handler
                            // Close();
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

                    await App.Current.ShowError($"Failed to download new version of UndertaleModTool.\nError - {errMsg}.");
                    IsBusy = false;
                }
            }
            catch (Exception e)
            {
                await App.Current.ShowError("Update Failed", $"An error occurred during the update process: {e.Message}");
            }
            finally
            {
                // Ensure the dialog is always hidden and the busy flag is cleared
                loadingDialog.Hide();
                IsBusy = false;
            }
            // Yay, now it is at least less connected to the views.
        }
    }
}
