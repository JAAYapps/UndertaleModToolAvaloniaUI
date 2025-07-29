using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia
{
    public static class AppConstants
    {
        // Version info
        public static string Edition = "(Git: " + GitVersion.GetGitVersion().Substring(0, 7) + ")";

        // On debug, build with git versions and provided release version. Otherwise, use the provided release version only.
#if DEBUG || SHOW_COMMIT_HASH
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString() + (Edition != "" ? " - " + Edition : "");
#else
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif

        private static UndertaleData? data = null;

        public static UndertaleData? Data { get => data; set => data = value; }

        public static string? FilePath { get; set; }
        
        public static string? ScriptPath { get; set; } // For the scripting interface specifically
        
        public static string LOCATION { get => AppDomain.CurrentDomain.BaseDirectory; }

        public static bool CrashedWhileEditing = false;

        public static string ProfileHash = null;

        public const string UndertaleStringFormat = "application/x-undertale-string";
        public const string UndertaleObjectFormat = "application/x-undertale-object";
    }
}
