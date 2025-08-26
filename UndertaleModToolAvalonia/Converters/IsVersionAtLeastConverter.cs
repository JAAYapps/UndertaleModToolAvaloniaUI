using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UndertaleModToolAvalonia.Converters
{
    public class IsVersionAtLeastConverter : IValueConverter
    {
        private static readonly Regex versionRegex = new(@"(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?", RegexOptions.Compiled);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (AppConstants.Data?.GeneralInfo is null
                || parameter is not string verStr
                || verStr.Length == 0)
                return false;

            var ver = versionRegex.Match(verStr);
            if (!ver.Success)
                return false;
            try
            {
                uint major = uint.Parse(ver.Groups[1].Value);
                uint minor = uint.Parse(ver.Groups[2].Value);
                uint release = 0;
                uint build = 0;
                if (ver.Groups[3].Value != "")
                    release = uint.Parse(ver.Groups[3].Value);
                if (ver.Groups[4].Value != "")
                    build = uint.Parse(ver.Groups[4].Value);

                if (AppConstants.Data.IsVersionAtLeast(major, minor, release, build))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
