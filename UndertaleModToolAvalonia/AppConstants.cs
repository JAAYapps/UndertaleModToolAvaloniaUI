using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;

namespace UndertaleModToolAvalonia
{
    public static class AppConstants
    {
        private static UndertaleData? data = null;

        public static UndertaleData? Data { get => data; set => data = value; }

        public static string? FilePath { get; set; }
        
        public static string? ScriptPath { get; set; } // For the scripting interface specifically
        
        public static string LOCATION { get => AppDomain.CurrentDomain.BaseDirectory; }
    }
}
