using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Models.EditorModels
{
    public class GlobalInit
    {
        public IList<UndertaleGlobalInit> GlobalInits { get; private set; }

        public GlobalInit(IList<UndertaleGlobalInit> globalInits)
        {
            this.GlobalInits = globalInits;
        }
    }
}
