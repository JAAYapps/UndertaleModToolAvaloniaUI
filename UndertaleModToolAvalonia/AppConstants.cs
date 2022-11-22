using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;

namespace UndertaleModToolAvalonia
{
    public class AppConstants
    {
        private static UndertaleData? data = null;

        public static UndertaleData? Data { get => data; set => data = value; }

        public static string LOCATION { get => AppDomain.CurrentDomain.BaseDirectory; }
    }
}
