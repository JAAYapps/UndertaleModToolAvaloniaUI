using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

namespace UndertaleModToolAvalonia.Messages
{
    public class GoToCodeLineMessage
    {
        public UndertaleCodeEditorViewModel Editor { get; }
        public int Line { get; }
        public UndertaleCodeEditorViewModel.CodeEditorTab Tab { get; }

        public GoToCodeLineMessage(UndertaleCodeEditorViewModel editor, int line, UndertaleCodeEditorViewModel.CodeEditorTab tab)
        {
            Editor = editor;
            Line = line;
            Tab = tab;
        }
    }
}