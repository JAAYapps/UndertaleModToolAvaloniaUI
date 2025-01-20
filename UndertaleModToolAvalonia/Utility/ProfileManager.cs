using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModToolAvalonia.ViewModels.EditorsViewModels;

namespace UndertaleModToolAvalonia.Utility;

public class ProfileManager
{
//     private static async void UpdateProfile(UndertaleData data, string filename)
//     {
//         await LoaderDialogFactory.UpdateProgressStatus("Calculating MD5 hash...");
//
//         try
//         {
//             await Task.Run(() =>
//             {
//                 using (var md5Instance = MD5.Create())
//                 {
//                     using (var stream = File.OpenRead(filename))
//                     {
//                         Settings.Instance.MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
//                         Settings.Instance.MD5PreviouslyLoaded = Settings.Instance.MD5CurrentlyLoaded;
//                         Settings.Instance.ProfileHash = BitConverter.ToString(Settings.Instance.MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
//                     }
//                 }
//             });
//
//             string profDir = Path.Combine(ProfilesFolder, ProfileHash);
//             string profDirTemp = Path.Combine(profDir, "Temp");
//             string profDirMain = Path.Combine(profDir, "Main");
//
//             if (ProfileViewModel.ProfileModeEnabled)
//             {
//                 Directory.CreateDirectory(ProfilesFolder);
//                 if (Directory.Exists(profDir))
//                 {
//                     if (!Directory.Exists(profDirTemp) && Directory.Exists(profDirMain))
//                     {
//                         // Get the subdirectories for the specified directory.
//                         DirectoryInfo dir = new DirectoryInfo(profDirMain);
//                         Directory.CreateDirectory(profDirTemp);
//                         // Get the files in the directory and copy them to the new location.
//                         FileInfo[] files = dir.GetFiles();
//                         foreach (FileInfo file in files)
//                         {
//                             string tempPath = Path.Combine(profDirTemp, file.Name);
//                             file.CopyTo(tempPath, false);
//                         }
//                     }
//                     else if (!Directory.Exists(profDirMain) && Directory.Exists(profDirTemp))
//                     {
//                         // Get the subdirectories for the specified directory.
//                         DirectoryInfo dir = new DirectoryInfo(profDirTemp);
//                         Directory.CreateDirectory(profDirMain);
//                         // Get the files in the directory and copy them to the new location.
//                         FileInfo[] files = dir.GetFiles();
//                         foreach (FileInfo file in files)
//                         {
//                             string tempPath = Path.Combine(profDirMain, file.Name);
//                             file.CopyTo(tempPath, false);
//                         }
//                     }
//
//                     // First generation no longer exists, it will be generated on demand while you edit.
//                     Directory.CreateDirectory(profDir);
//                     Directory.CreateDirectory(profDirMain);
//                     Directory.CreateDirectory(profDirTemp);
//                     if (!Directory.Exists(profDir) || !Directory.Exists(profDirMain) || !Directory.Exists(profDirTemp))
//                     {
//                         Application.Current.ShowMessage("Profile should exist, but does not. Insufficient permissions? Profile mode is disabled.");
//                         ProfileViewModel.ProfileModeEnabled = false;
//                         return;
//                     }
//
//                     if (!ProfileViewModel.ProfileMessageShown)
//                     {
//                         Application.Current.ShowMessage(@"The profile for your game loaded successfully!
//
// UndertaleModTool now uses the ""Profile"" system by default for code.
// Using the profile system, many new features are available to you!
// For example, the code is fully editable (you can even add comments)
// and it will be saved exactly as you wrote it. In addition, if the
// program crashes or your computer loses power during editing, your
// code edits will be recovered automatically the next time you start
// the program.
//
// The profile system can be toggled on or off at any time by going
// to the ""File"" tab at the top and then opening the ""Settings""
// (the ""Enable profile mode"" option toggles it on or off).
// You may wish to disable it for purposes such as collaborative
// modding projects, or when performing technical operations.
// For more in depth information, please read ""About_Profile_Mode.txt"".
//
// It should be noted that this system is somewhat experimental, so
// should you encounter any problems, please let us know or leave
// an issue on GitHub.");
//                         ProfileViewModel.ProfileMessageShown = true;
//                     }
//                     CreateUMTLastEdited(filename);
//                 }
//             }
//         }
//         catch (Exception)
//         {
//
//             throw;
//         }
//     }
}