using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    internal class AudioToStringConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3)
                return string.Empty;

            var audioId = values[0] as int?;
            var groupId = values[1] as int?;
            var groupName = values[2] as string; // From GroupReference.Name.Content

            if (audioId == -1) return "(null)";

            if (!string.IsNullOrEmpty(groupName))
            {
                return $"(UndertaleEmbeddedAudio#{audioId} in UndertaleAudioGroup#{groupId}:{groupName})";
            }

            return $"(UndertaleEmbeddedAudio#{audioId})";
        }
    }
}
