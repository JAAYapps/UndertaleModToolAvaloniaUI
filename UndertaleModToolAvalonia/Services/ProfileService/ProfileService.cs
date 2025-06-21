using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Services.ProfileService
{
    internal class ProfileService(ILoadingDialogService loadingDialogService) : IProfileService
    {
        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            return GetDecompiledText(AppConstants.Data.Code.ByName(codeName), context, settings);
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            if (code.ParentEntry is not null)
                return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", decompile that instead.";

            GlobalDecompileContext globalDecompileContext = context is null ? new(AppConstants.Data) : context;
            try
            {
                return code != null
                    ? new DecompileContext(globalDecompileContext, code, settings ?? AppConstants.Data.ToolInfo.DecompilerSettings).DecompileToString()
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
                return code != null ? code.Disassemble(AppConstants.Data.Variables, AppConstants.Data.CodeLocals?.For(code), AppConstants.Data.CodeLocals is null) : "";
            }
            catch (Exception e)
            {
                return "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"; // Please don't
            }
        }
        public string GetDisassemblyText(string codeName)
        {
            return GetDisassemblyText(AppConstants.Data.Code.ByName(codeName));
        }

        public async Task CrashCheckAsync()
        {
            if (!Settings.Instance.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                string lastEditedLocation = Path.Combine(Settings.ProfilesFolder, "LastEdited.txt");
                if (AppConstants.Data == null && File.Exists(lastEditedLocation))
                {
                    AppConstants.CrashedWhileEditing = true;
                    string[] crashRecoveryData = File.ReadAllText(lastEditedLocation).Split('\n');
                    string dataRecoverLocation = Path.Combine(Settings.ProfilesFolder, crashRecoveryData[0].Trim(), "Temp");
                    string profileHashOfCrashedFile;
                    string reportedHashOfCrashedFile = crashRecoveryData[0].Trim();
                    string pathOfCrashedFile = crashRecoveryData[1];
                    string pathOfRecoverableCode = Path.Combine(Settings.ProfilesFolder, reportedHashOfCrashedFile);
                    if (File.Exists(pathOfCrashedFile))
                    {
                        using (var md5Instance = MD5.Create())
                        {
                            using (var stream = File.OpenRead(pathOfCrashedFile))
                            {
                                profileHashOfCrashedFile = BitConverter.ToString(md5Instance.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                            }
                        }
                        if (Directory.Exists(Path.Combine(Settings.ProfilesFolder, reportedHashOfCrashedFile)) &&
                            profileHashOfCrashedFile == reportedHashOfCrashedFile)
                        {
                            if (await App.Current.ShowQuestion("UndertaleModTool crashed during usage last time while editing " + pathOfCrashedFile + ". Profile mode code from that session still exists. Would you like to move the code to the \"Recovered\" folder now? Any previous code there will be cleared!") == MsBox.Avalonia.Enums.ButtonResult.Yes)
                            {
                                await App.Current.ShowMessage("Your code can be recovered from the \"Recovered\" folder at any time.");
                                string recoveredDir = Path.Combine(Settings.AppDataFolder, "Recovered", reportedHashOfCrashedFile);
                                if (!Directory.Exists(Path.Combine(Settings.AppDataFolder, "Recovered")))
                                    Directory.CreateDirectory(Path.Combine(Settings.AppDataFolder, "Recovered"));
                                if (Directory.Exists(recoveredDir))
                                    Directory.Delete(recoveredDir, true);
                                Directory.Move(pathOfRecoverableCode, recoveredDir);
                                await ApplyCorrectionsAsync();
                            }
                            else
                            {
                                await App.Current.ShowWarning("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                            }
                        }
                    }
                    else
                    {
                        await App.Current.ShowWarning("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                    }
                }
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("CrashCheck error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public async Task ApplyCorrectionsAsync()
        {
            if (!Settings.Instance.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                await DirectoryCopyAsync(Settings.CorrectionsFolder, Settings.ProfilesFolder, true);
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("ApplyCorrections error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public async Task CreateUMTLastEditedAsync(string filename)
        {
            if (!Settings.Instance.ProfileModeEnabled || AppConstants.ProfileHash is null)
            {
                return;
            }

            try
            {
                File.WriteAllText(Path.Combine(Settings.ProfilesFolder, "LastEdited.txt"), AppConstants.ProfileHash + "\n" + filename);
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("CreateUMTLastEdited error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public async Task DestroyUMTLastEditedAsync()
        {
            if (!Settings.Instance.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                string path = Path.Combine(Settings.ProfilesFolder, "LastEdited.txt");
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("DestroyUMTLastEdited error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public async Task RevertProfileAsync()
        {
            if (!Settings.Instance.ProfileModeEnabled || AppConstants.ProfileHash is null)
            {
                return;
            }

            try
            {
                string mainFolder = Path.Combine(Settings.ProfilesFolder, AppConstants.ProfileHash, "Main");
                Directory.CreateDirectory(mainFolder);
                string tempFolder = Path.Combine(Settings.ProfilesFolder, AppConstants.ProfileHash, "Temp");
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
                await DirectoryCopyAsync(mainFolder, tempFolder, true);
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("RevertProfile error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }
        public async Task UpdateProfileAsync(UndertaleData data, string filename)
        {
            if (!Settings.Instance.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                loadingDialogService.Show("Loading", "Loading, please wait...");
                await loadingDialogService.UpdateStatusAsync("Calculating MD5 hash...");

                await Task.Run(() =>
                {
                    using var md5Instance = MD5.Create();
                    using var stream = File.OpenRead(filename);
                    Settings.Instance.MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                    Settings.Instance.MD5PreviouslyLoaded = Settings.Instance.MD5CurrentlyLoaded;
                    AppConstants.ProfileHash = BitConverter.ToString(Settings.Instance.MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                });

                string profDir = Path.Combine(Settings.ProfilesFolder, AppConstants.ProfileHash);
                string profDirTemp = Path.Combine(profDir, "Temp");
                string profDirMain = Path.Combine(profDir, "Main");

                Directory.CreateDirectory(Settings.ProfilesFolder);
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
                }

                // First generation no longer exists, it will be generated on demand while you edit.
                Directory.CreateDirectory(profDir);
                Directory.CreateDirectory(profDirMain);
                Directory.CreateDirectory(profDirTemp);
                if (!Directory.Exists(profDir) || !Directory.Exists(profDirMain) || !Directory.Exists(profDirTemp))
                {
                    await App.Current.ShowWarning("Profile should exist, but does not. Insufficient permissions? Profile mode is disabled.");
                    Settings.Instance.ProfileModeEnabled = false;
                    return;
                }

                if (!Settings.Instance.ProfileMessageShown)
                {
                    await App.Current.ShowMessage(@"The profile for your game loaded successfully!

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
Be warned that scripts are likely to mess with this system,
and that enabling the profile mode setting won't have an immediate
effect. (You must re-open a game first.)

It should be noted that this system is somewhat experimental, so
should you encounter any problems, please let us know or leave
an issue on GitHub.");
                    Settings.Instance.ProfileMessageShown = true;
                }
                await CreateUMTLastEditedAsync(filename);
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("UpdateProfile error! (Note that profile mode is highly experimental.)\n" + exc);
            }
            finally
            {
                loadingDialogService.Hide();
            }
        }
        public async Task ProfileSaveEventAsync(UndertaleData data, string filename)
        {
            if (!Settings.Instance.ProfileModeEnabled || AppConstants.ProfileHash is null)
            {
                return;
            }

            try
            {
                loadingDialogService.Show("Loading", "Loading, please wait...");
                await loadingDialogService.UpdateStatusAsync("Calculating MD5 hash...");

                string deleteIfModeActive = BitConverter.ToString(Settings.Instance.MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                bool copyProfile = false;
                await Task.Run(() =>
                {
                    using var md5Instance = MD5.Create();
                    using var stream = File.OpenRead(filename);
                    Settings.Instance.MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                    if (!BitConverter.ToString(Settings.Instance.MD5PreviouslyLoaded).Replace("-", "").Equals(BitConverter.ToString(Settings.Instance.MD5CurrentlyLoaded).Replace("-", ""), StringComparison.InvariantCultureIgnoreCase))
                    {
                        copyProfile = true;
                    }
                });

                Directory.CreateDirectory(Path.Combine(Settings.ProfilesFolder, AppConstants.ProfileHash, "Main"));
                Directory.CreateDirectory(Path.Combine(Settings.ProfilesFolder, AppConstants.ProfileHash, "Temp"));
                string profDir;
                string MD5DirNameOld;
                string MD5DirPathOld;
                string MD5DirPathOldMain;
                string MD5DirPathOldTemp;
                string MD5DirNameNew;
                string MD5DirPathNew;
                if (copyProfile)
                {
                    MD5DirNameOld = BitConverter.ToString(Settings.Instance.MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                    MD5DirPathOld = Path.Combine(Settings.ProfilesFolder, MD5DirNameOld);
                    MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                    MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                    MD5DirNameNew = BitConverter.ToString(Settings.Instance.MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                    MD5DirPathNew = Path.Combine(Settings.ProfilesFolder, MD5DirNameNew);
                    await DirectoryCopyAsync(MD5DirPathOld, MD5DirPathNew, true);
                    if (Directory.Exists(MD5DirPathOldMain) && Directory.Exists(MD5DirPathOldTemp))
                    {
                        Directory.Delete(MD5DirPathOldTemp, true);
                    }
                    await DirectoryCopyAsync(MD5DirPathOldMain, MD5DirPathOldTemp, true);
                }
                Settings.Instance.MD5PreviouslyLoaded = Settings.Instance.MD5CurrentlyLoaded;
                // Get the subdirectories for the specified directory.
                MD5DirNameOld = BitConverter.ToString(Settings.Instance.MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                MD5DirPathOld = Path.Combine(Settings.ProfilesFolder, MD5DirNameOld);
                MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                if ((Directory.Exists(MD5DirPathOldMain)) && (Directory.Exists(MD5DirPathOldTemp)) && copyProfile)
                {
                    Directory.Delete(MD5DirPathOldMain, true);
                }
                await DirectoryCopyAsync(MD5DirPathOldTemp, MD5DirPathOldMain, true);

                AppConstants.ProfileHash = BitConverter.ToString(Settings.Instance.MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                profDir = Path.Combine(Settings.ProfilesFolder, AppConstants.ProfileHash);
                Directory.CreateDirectory(profDir);
                Directory.CreateDirectory(Path.Combine(profDir, "Main"));
                Directory.CreateDirectory(Path.Combine(profDir, "Temp"));

                if (Settings.Instance.DeleteOldProfileOnSave && copyProfile)
                {
                    Directory.Delete(Path.Combine(Settings.ProfilesFolder, deleteIfModeActive), true);
                }
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("ProfileSaveEvent error! (Note that profile mode is highly experimental.)\n" + exc);
            }
            finally
            {
                loadingDialogService.Hide();
            }
        }
        public async Task DirectoryCopyAsync(string sourceDirName, string destDirName, bool copySubDirs)
        {
            try
            {
                // Get the subdirectories for the specified directory.
                DirectoryInfo dir = new DirectoryInfo(sourceDirName);

                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
                }

                DirectoryInfo[] dirs = dir.GetDirectories();

                // If the destination directory doesn't exist, create it.
                Directory.CreateDirectory(destDirName);

                // Get the files in the directory and copy them to the new location.
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(destDirName, file.Name);
                    if (!File.Exists(tempPath))
                    {
                        try
                        {
                            file.CopyTo(tempPath, false);
                        }
                        catch (Exception ex)
                        {
                            await App.Current.ShowError("An exception occurred while processing copying " + tempPath + "\nException: \n" + ex);
                            return;
                        }
                    }
                }

                // If copying subdirectories, copy them and their contents to new location.
                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        string tempPath = Path.Combine(destDirName, subdir.Name);
                        await DirectoryCopyAsync(subdir.FullName, tempPath, copySubDirs);
                    }
                }
            }
            catch (Exception exc)
            {
                await App.Current.ShowError("DirectoryCopy error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }
    }
}
