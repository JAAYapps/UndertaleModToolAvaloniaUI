using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Utilities
{
    public class EnumValuesExtension : MarkupExtension
    {
        public Type? EnumType { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (EnumType is null || !EnumType.IsEnum)
            {
                throw new ArgumentException("Property 'EnumType' must be set to a valid enum type.");
            }

            return Enum.GetValues(EnumType);
        }
    }
}
