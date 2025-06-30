using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Models.EditorModels
{
    public class GameEnd
    {
        public IList<UndertaleGlobalInit> GameEnds { get; private set; }

        public GameEnd(IList<UndertaleGlobalInit> GameEnds)
        {
            this.GameEnds = GameEnds;
        }
    }
}
