using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.Models.UndertaleReferenceTypes
{
    public class HashSetTypesOverride : HashSet<Type>
    {
        private readonly bool containsEverything, isYYC;
        public HashSetTypesOverride(bool containsEverything = false, bool isYYC = false)
        {
            this.containsEverything = containsEverything;
            this.isYYC = isYYC;
        }
        public new bool Contains(Type item)
        {
            if (!containsEverything)
                return base.Contains(item);

            return !isYYC || !UndertaleResourceReferenceMap.CodeTypes.Contains(item);
        }
    }
}
