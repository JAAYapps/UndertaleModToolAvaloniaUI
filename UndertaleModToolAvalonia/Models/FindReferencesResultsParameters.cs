using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;

namespace UndertaleModToolAvalonia.Models
{
    public record FindReferencesResultsParameters(UndertaleResource? undertaleResource, UndertaleData data, Dictionary<string, List<object>> results);
}
