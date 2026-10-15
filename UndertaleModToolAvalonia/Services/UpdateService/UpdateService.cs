using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Services.UpdateService
{
    public partial class UpdateService : ObservableObject, IUpdateService
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/JAAYapps/UndertaleModToolAvaloniaUI/releases/latest";
        private const string ReleasesPageUrl = "https://github.com/JAAYapps/UndertaleModToolAvaloniaUI/releases";

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

        private static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

        private static Version Normalize(Version version)
        {
            return new Version(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
        }

        private static bool TryParseTag(string tag, out Version version)
        {
            if (Version.TryParse(tag.Trim().TrimStart('v', 'V'), out Version? parsed))
            {
                version = Normalize(parsed);
                return true;
            }
            version = new Version(0, 0);
            return false;
        }

        public async Task CheckForUpdatesAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                loadingDialog.Show("Checking for updates...", "Contacting GitHub...");

                Version current = Normalize(CurrentVersion);
                HttpResponseMessage? response;
                try
                {
                    using HttpRequestMessage request = new(HttpMethod.Get, LatestReleaseUrl);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                    request.Headers.UserAgent.Add(new ProductInfoHeaderValue("UndertaleModTool", current.ToString()));
                    response = await httpClient.SendAsync(request);
                }
                catch (Exception)
                {
                    response = null;
                }
                loadingDialog.Hide();

                if (response is null)
                {
                    await App.Current!.ShowError("Could not contact GitHub to check for updates.\nCheck your internet connection and try again.", "Update check failed");
                    return;
                }
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await App.Current!.ShowMessage($"No releases have been published yet.\nYou have version {current}.", "Check for updates");
                    return;
                }
                if (!response.IsSuccessStatusCode)
                {
                    await App.Current!.ShowError($"GitHub answered with {(int)response.StatusCode} {response.ReasonPhrase}.\nTry again later.", "Update check failed");
                    return;
                }

                JObject release = JObject.Parse(await response.Content.ReadAsStringAsync());
                string tag = (string?)release["tag_name"] ?? "";
                string pageUrl = (string?)release["html_url"] ?? ReleasesPageUrl;

                if (!TryParseTag(tag, out Version latest))
                {
                    if (await App.Current!.ShowQuestion($"The latest release is named \"{tag}\", which is not a version number, so it cannot be compared with your version {current}.\n\nOpen the release page?", Icon.Question, "Check for updates") == ButtonResult.Yes)
                        await UndertaleHelper.OpenBrowser(pageUrl);
                    return;
                }

                if (latest > current)
                {
                    if (await App.Current!.ShowQuestion($"Version {latest} is available. You have version {current}.\n\nOpen the download page?", Icon.Question, "Update available") == ButtonResult.Yes)
                        await UndertaleHelper.OpenBrowser(pageUrl);
                }
                else
                {
                    await App.Current!.ShowMessage($"You have the latest version ({current}).", "Check for updates");
                }
            }
            catch (Exception e)
            {
                await App.Current!.ShowError($"An error occurred while checking for updates: {e.Message}", "Update check failed");
            }
            finally
            {
                loadingDialog.Hide();
                IsBusy = false;
            }
        }
    }
}
