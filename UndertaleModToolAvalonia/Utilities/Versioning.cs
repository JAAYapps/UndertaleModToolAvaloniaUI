using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Utilities
{
    public static class Versioning
    {
        public static readonly AttachedProperty<string?> MinVersionProperty =
            AvaloniaProperty.RegisterAttached<Control, string?>("MinVersion", typeof(Versioning));

        public static string? GetMinVersion(Control element) => element.GetValue(MinVersionProperty);
        public static void SetMinVersion(Control element, string? value) => element.SetValue(MinVersionProperty, value);

        static Versioning()
        {
            MinVersionProperty.Changed.AddClassHandler<Control>((control, args) =>
            {
                HandleVersionCheck(control);
            });
        }

        private static void HandleVersionCheck(Control control)
        {
            var versionStr = GetMinVersion(control);
            if (string.IsNullOrEmpty(versionStr))
            {
                control.Classes.Set(":version-met", false);
                return;
            }

            var converter = new Converters.IsVersionAtLeastConverter();
            bool isAtLeast = (bool)converter.Convert(null, typeof(bool), versionStr, CultureInfo.InvariantCulture);

            control.Classes.Set(":version-met", isAtLeast);
        }
    }
}
