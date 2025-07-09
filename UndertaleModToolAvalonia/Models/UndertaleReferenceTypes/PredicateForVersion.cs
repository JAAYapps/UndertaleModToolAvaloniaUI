using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Models.UndertaleReferenceTypes
{
    public class PredicateForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public (uint Major, uint Minor, uint Release) BeforeVersion { get; set; } = (uint.MaxValue, uint.MaxValue, uint.MaxValue);
        public bool DisableForLTS2022 { get; set; } = false;
        public Func<object, HashSetTypesOverride, bool, Dictionary<string, object[]>> Predicate { get; set; }
    }
}
