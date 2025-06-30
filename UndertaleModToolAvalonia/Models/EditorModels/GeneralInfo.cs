using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Models.EditorModels
{
    public class GeneralInfo
    {
        public UndertaleGeneralInfo Info { get; private set; }
        public UndertaleOptions Options { get; private set; }
        public UndertaleLanguage Language { get; private set; }

        public GeneralInfo(UndertaleGeneralInfo generalInfo, UndertaleOptions options, UndertaleLanguage language)
        {
            this.Info = generalInfo;
            this.Options = options;
            this.Language = language;
        }
    }
}
