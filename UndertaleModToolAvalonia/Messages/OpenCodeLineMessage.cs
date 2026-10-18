using UndertaleModLib.Models;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Messages
{
    public class OpenCodeLineMessage
    {
        public UndertaleCode Code { get; }
        public int Line { get; }
        public UndertaleCodeEditorViewModel.CodeEditorTab Tab { get; }
        public bool InNewTab { get; }

        public OpenCodeLineMessage(UndertaleCode code, int line, UndertaleCodeEditorViewModel.CodeEditorTab tab, bool inNewTab)
        {
            Code = code;
            Line = line;
            Tab = tab;
            InNewTab = inNewTab;
        }
    }
}