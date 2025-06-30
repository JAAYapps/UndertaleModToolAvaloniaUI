using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Utilities
{
    public static class EnumerableExtensions
    {
        public static T[] ToEmptyArray<T>(this IEnumerable<T> _) => Array.Empty<T>();
    }
}
