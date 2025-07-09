using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Models.UndertaleReferenceTypes
{
    public class TypesForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public (uint Major, uint Minor, uint Release) BeforeVersion { get; set; } = (uint.MaxValue, uint.MaxValue, uint.MaxValue);
        public (Type, string)[] Types { get; set; }
    }
}
