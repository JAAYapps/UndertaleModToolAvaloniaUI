using AAYInvisionaryTTSPlayer.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using MsBox.Avalonia.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;
using static UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents.UndertaleCodeEditorViewModel;

namespace UndertaleModToolAvalonia.Views.EditorViews.EditorComponents;

public partial class UndertaleCodeEditorView : UserControl
{
    public SearchPanel DecompiledSearchPanel;
    public SearchPanel DisassemblySearchPanel;

    public UndertaleCodeEditorView()
    {
        InitializeComponent();
        DecompiledEditor.LostFocus += DecompiledEditor_LostFocus;
        DecompiledEditor.GotFocus += DecompiledEditor_GotFocus;
        DisassemblyEditor.LostFocus += DisassemblyEditor_LostFocus;
        DisassemblyEditor.GotFocus += DisassemblyEditor_GotFocus;
        CodeModeTabs.SelectionChanged += TabControl_SelectionChanged;
        this.DataContextChanged += UserControl_DataContextChanged;
        this.Loaded += UndertaleCodeEditor_Loaded;
        this.Unloaded += UndertaleCodeEditor_Unloaded;

        // Decompiled editor styling and functionality
        DecompiledSearchPanel = SearchPanel.Install(DecompiledEditor);
        DecompiledSearchPanel.LostFocus += SearchPanel_LostFocus;
        DecompiledSearchPanel.SetSearchResultsBrush(new SolidColorBrush(Color.FromRgb(90, 90, 90)));

        // Load the GML.xshd file from embedded resources
        using (Stream stream = AssetGrabber.GetAssetStream("GML.xshd"))
        {
            if (stream == null) return;
            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                DecompiledEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                var def = DecompiledEditor.SyntaxHighlighting;
                if (AppConstants.Data?.GeneralInfo.Major < 2)
                {
                    foreach (var span in def.MainRuleSet.Spans)
                    {
                        string expr = span.StartExpression.ToString();
                        if (expr == "\"" || expr == "'")
                        {
                            span.RuleSet.Spans.Clear();
                        }
                    }
                }
                // This was an attempt to only highlight
                // GMS 2.3+ keywords if the game is
                // made in such a version.
                // However despite what StackOverflow
                // says, this isn't working so it's just
                // hardcoded in the XML for now
                /*
                if(mainWindow.Data.IsVersionAtLeast(2, 3))
                {
                    HighlightingColor color = null;
                    foreach (var rule in def.MainRuleSet.Rules)
                    {
                        if (rule.Regex.IsMatch("if"))
                        {
                            color = rule.Color;
                            break;
                        }
                    }
                    if (color != null)
                    {
                        string[] keywords =
                        {
                            "new",
                            "function",
                            "keywords"
                        };
                        var rule = new HighlightingRule();
                        var regex = String.Format(@"\b(?>{0})\b", String.Join("|", keywords));

                        rule.Regex = new Regex(regex);
                        rule.Color = color;

                        def.MainRuleSet.Rules.Add(rule);
                    }
                }*/
            }
        }

        DecompiledEditor.Options.ConvertTabsToSpaces = true;

        TextArea textArea = DecompiledEditor.TextArea;
        textArea.TextView.ElementGenerators.Add(new NumberGenerator(this, textArea));
        textArea.TextView.ElementGenerators.Add(new NameGenerator(this, textArea));

        textArea.TextView.Options.HighlightCurrentLine = true;
        textArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        textArea.TextView.CurrentLineBorder = new Pen() { Thickness = 0 };

        DecompiledEditor.Document.TextChanged += (s, e) =>
        {
            if (DataContext is not UndertaleCodeEditorViewModel vm)
                return;

            vm.DecompiledFocused = true;
            vm.DecompiledChanged = true;
        };

        textArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        textArea.SelectionForeground = null;
        textArea.SelectionBorder = null;
        textArea.SelectionCornerRadius = 0;

        // Disassembly editor styling and functionality
        DisassemblySearchPanel = SearchPanel.Install(DisassemblyEditor);
        DisassemblySearchPanel.LostFocus += SearchPanel_LostFocus;
        DisassemblySearchPanel.SetSearchResultsBrush(new SolidColorBrush(Color.FromRgb(90, 90, 90)));

        using (var stream = AssetGrabber.GetAssetStream("VMASM.xshd"))
        {
            if (stream == null) return;
            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                DisassemblyEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }

        textArea = DisassemblyEditor.TextArea;
        textArea.TextView.ElementGenerators.Add(new NameGenerator(this, textArea));

        textArea.TextView.Options.HighlightCurrentLine = true;
        textArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        textArea.TextView.CurrentLineBorder = new Pen() { Thickness = 0 };

        DisassemblyEditor.Document.TextChanged += (s, e) =>
        {
            if (DataContext is not UndertaleCodeEditorViewModel vm)
                return;

            vm.DisassemblyChanged = true;
        };

        textArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        textArea.SelectionForeground = null;
        textArea.SelectionBorder = null;
        textArea.SelectionCornerRadius = 0;
    }

    private void SearchPanel_LostFocus(object? sender, RoutedEventArgs e)
    {
        SearchPanel panel = sender as SearchPanel;
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance;
        FieldInfo toolTipField = typeof(SearchPanel).GetField("messageView", flags);
        if (toolTipField is null)
        {
            Debug.WriteLine("The source code of \"AvaloniaEdit.Search.SearchPanel\" was changed - can't find \"messageView\" field.");
            return;
        }

        ToolTip noMatchesTT = toolTipField.GetValue(panel) as ToolTip;
        if (noMatchesTT is null)
        {
            Debug.WriteLine("Can't get an instance of the \"SearchPanel.messageView\" popup.");
            return;
        }

        noMatchesTT.IsVisible = false;
    }

    private void UndertaleCodeEditor_Unloaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        vm.OverriddenDecompPos = default;
        vm.OverriddenDisasmPos = default;
    }

    private async void UndertaleCodeEditor_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleCodeEditorViewModel)
            await FillInCodeViewer();
    }

    private async Task FillInCodeViewer(bool overrideFirst = false)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm) 
            return;
        UndertaleCode code = vm.UndertaleCode;
        if (DisassemblyTab.IsSelected && code != vm.CurrentDisassembled)
        {
            if (!overrideFirst)
            {
                DisassembleCode(code, !vm.DisassembledYet);
                vm.DisassembledYet = true;
            }
            else
                DisassembleCode(code, true);
        }
        if (DecompiledTab.IsSelected && code != vm.CurrentDecompiled)
        {
            if (!overrideFirst)
            {
                await DecompileCode(code, !vm.DecompiledYet);
                vm.DecompiledYet = true;
            }
            else
                await DecompileCode(code, true);
        }
        if (DecompiledTab.IsSelected)
        {
            // Re-populate local variables when in decompiled code, fixing #1320
            vm.PopulateCurrentLocals(AppConstants.Data!, code);
        }
    }

    private async void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;
        UndertaleCode code = vm.UndertaleCode;
        if (code == null)
            return;

        DecompiledSearchPanel.Close();
        DisassemblySearchPanel.Close();

        await DecompiledLostFocusBody(sender, null);
        DisassemblyEditor_LostFocus(sender, null);

        if (!IsLoaded)
        {
            // If it's not loaded, then "FillInCodeViewer()" will be executed on load.
            // This prevents a bug with freezing on code opening.
            return;
        }

        await FillInCodeViewer();
    }

    private async void UserControl_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;
        UndertaleCode code = vm.UndertaleCode;
        if (code == null)
            return;

        vm.FillObjectDicts();

        // compile/disassemble previously edited code (save changes)
        if (DecompiledTab.IsSelected && vm.DecompiledFocused && vm.DecompiledChanged &&
            vm.CurrentDecompiled is not null && vm.CurrentDecompiled != code)
        {
            vm.DecompiledSkipped = true;
            await DecompiledLostFocusBody(sender, null);
        }
        else if (DisassemblyTab.IsSelected && vm.DisassemblyFocused && vm.DisassemblyChanged &&
                 vm.CurrentDisassembled is not null && vm.CurrentDisassembled != code)
        {
            vm.DisassemblySkipped = true;
            DisassemblyEditor_LostFocus(sender, null);
        }

        await DecompiledLostFocusBody(sender, null);
        DisassemblyEditor_LostFocus(sender, null);

        vm.DecompiledYet = false;
        vm.DisassembledYet = false;
        vm.CurrentDecompiled = null;
        vm.CurrentDisassembled = null;

        if (vm.EditorTab != CodeEditorTab.Unknown) // If opened from the code search results "link"
        {
            if (vm.EditorTab == CodeEditorTab.Disassembly && code != vm.CurrentDisassembled)
            {
                if (CodeModeTabs.SelectedItem != DisassemblyTab)
                    CodeModeTabs.SelectedItem = DisassemblyTab;
                else
                    DisassembleCode(code, true);
            }

            if (vm.EditorTab == CodeEditorTab.Decompiled && code != vm.CurrentDecompiled)
            {
                if (CodeModeTabs.SelectedItem != DecompiledTab)
                    CodeModeTabs.SelectedItem = DecompiledTab;
                else
                    _ = DecompileCode(code, true);
            }

            vm.EditorTab = CodeEditorTab.Unknown;
        }
        else
            await FillInCodeViewer(true);
    }

    private async Task CompileCommandBody(object sender, EventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (vm.DecompiledFocused)
        {
            await DecompiledLostFocusBody(sender, new RoutedEventArgs());
        }
        else if (vm.DisassemblyFocused)
        {
            DisassemblyEditor_LostFocus(sender, new RoutedEventArgs());
            DisassemblyEditor_GotFocus(sender, null);
        }

        await Task.Delay(1); //dummy await
    }

    private async Task Command_Compile(object sender, EventArgs e)
    {
        await CompileCommandBody(sender, e);
    }
    public async Task SaveChanges()
    {
        await CompileCommandBody(null, null);
    }

    private void DecompiledEditor_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (vm.IsReadOnly)
            return;
        vm.DecompiledFocused = true;
    }

    private async void DecompiledEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        await DecompiledLostFocusBody(sender, e);
    }

    private void DisassemblyEditor_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (vm.IsReadOnly)
            return;
        vm.DisassemblyFocused = true;
    }

    private void DisassemblyEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (!vm.DisassemblyFocused)
            return;
        if (vm.IsReadOnly)
            return;
        vm.DisassemblyFocused = false;

        if (!vm.DisassemblyChanged)
            return;

        UndertaleCode code;
        if (vm.DisassemblySkipped)
        {
            code = vm.CurrentDisassembled;
            vm.DisassemblySkipped = false;
        }
        else
            code = vm.UndertaleCode;

        if (code == null)
        {
            if (IsLoaded)
                code = vm.CurrentDisassembled; // switched to the tab with different object type
            else
                return;                     // probably loaded another data.win or something.
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.FocusManager == null) return;

        var currentlyFocused = topLevel.FocusManager.GetFocusedElement();

        if (currentlyFocused != null && DecompiledEditor.IsVisualAncestorOf(currentlyFocused as Visual))
            return;

        UndertaleData data = AppConstants.Data;
        try
        {
            var instructions = Assembler.Assemble(DisassemblyEditor.Text, data);
            code.Replace(instructions);
        }
        catch (Exception ex)
        {
            _ = App.Current!.ShowError(ex.ToString(), "Assembler error");
            return;
        }

        // Get rid of old code
        vm.CurrentDisassembled = null;
        vm.CurrentDecompiled = null;

        // Tab switch
        if (e == null)
            return;

        // Disassemble new code
        DisassembleCode(code, false);

        if (!DisassemblyEditor.IsReadOnly)
        {
            if (vm.Editor.IsSaving)
            {
                vm.Editor.IsSaving = false;

                vm.fileService.SaveFileAsync(TopLevel.GetTopLevel(this)!.StorageProvider);
            }
        }
    }

    private void DisassembleCode(UndertaleCode code, bool first)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        string text;

        int currLine = 1;
        int currColumn = 1;
        double scrollPos = 0;
        if (!first)
        {
            var caret = DisassemblyEditor.TextArea.Caret;
            currLine = caret.Line;
            currColumn = caret.Column;
            scrollPos = DisassemblyEditor.VerticalOffset;
        }
        else if (vm.OverriddenDisasmPos != default)
        {
            currLine = vm.OverriddenDisasmPos.Line;
            currColumn = vm.OverriddenDisasmPos.Column;
            scrollPos = vm.OverriddenDisasmPos.ScrollPos;

            vm.OverriddenDisasmPos = default;
        }

        DisassemblyEditor.TextArea.ClearSelection();
        if (code.ParentEntry != null)
        {
            DisassemblyEditor.IsReadOnly = true;
            text = "; This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there.";
            vm.DisassemblyChanged = false;
        }
        else
        {
            DisassemblyEditor.IsReadOnly = false;

            try
            {
                var data = AppConstants.Data;
                text = code.Disassemble(data.Variables, data.CodeLocals?.For(code), data.CodeLocals is null);

                vm.CurrentLocals.Clear();
            }
            catch (Exception ex)
            {
                DisassemblyEditor.IsReadOnly = true;

                string exStr = ex.ToString();
                exStr = String.Join("\n;", exStr.Split('\n'));
                text = $";  EXCEPTION!\n;   {exStr}\n";
            }
        }

        DisassemblyEditor.Document.BeginUpdate();
        DisassemblyEditor.Document.Text = text;

        if (!DisassemblyEditor.IsReadOnly)
            RestoreCaretPosition(DisassemblyEditor, currLine, currColumn, scrollPos);

        DisassemblyEditor.Document.EndUpdate();

        if (first)
            DisassemblyEditor.Document.UndoStack.ClearAll();

        vm.CurrentDisassembled = code;
        vm.DisassemblyChanged = false;
    }

    private async Task DecompileCode(UndertaleCode code, bool first)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        vm.IsReadOnly = true;

        int currLine = 1;
        int currColumn = 1;
        double scrollPos = 0;
        if (!first)
        {
            var caret = DecompiledEditor.TextArea.Caret;
            currLine = caret.Line;
            currColumn = caret.Column;
            scrollPos = DecompiledEditor.VerticalOffset;
        }
        else if (vm.OverriddenDecompPos != default)
        {
            currLine = vm.OverriddenDecompPos.Line;
            currColumn = vm.OverriddenDecompPos.Column;
            scrollPos = vm.OverriddenDecompPos.ScrollPos;

            vm.OverriddenDecompPos = default;
        }

        DecompiledEditor.TextArea.ClearSelection();

        if (code.ParentEntry != null)
        {
            DecompiledEditor.Text = "// This code entry is a reference to an anonymous function within " + code.ParentEntry.Name.Content + ", view it there.";
            vm.DecompiledChanged = false;
            vm.CurrentDecompiled = code;
            vm.loadingDialogSerivce.Hide();
        }
        else
        {
            vm.loadingDialogSerivce.Show("Decompiling", "Decompiling, please wait... This can take a while on complex scripts.");

            bool openSaveDialog = false;

            UndertaleCode gettextCode = null;
            if (vm.Gettext == null)
                gettextCode = AppConstants.Data!.Code.ByName("gml_Script_textdata_en");

            string dataPath = Path.GetDirectoryName(AppConstants.FilePath);
            string gettextJsonPath = null;
            if (dataPath is not null)
            {
                gettextJsonPath = Path.Combine(dataPath, "lang", "lang_en.json");
                if (!File.Exists(gettextJsonPath))
                    gettextJsonPath = Path.Combine(dataPath, "lang", "lang_en_ch1.json");
            }

            var dataa = AppConstants.Data;
            await Task.Run(async () =>
            {
                var result = await vm.CompileAsync(code, dataa, gettextCode, gettextJsonPath);

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (vm.UndertaleCode != code)
                        return; // Switched to another code entry or otherwise

                    DecompiledEditor.Document.BeginUpdate();
                    if (result.e != null)
                        DecompiledEditor.Document.Text = "/* EXCEPTION!\n   " + result.e.ToString() + "\n*/";
                    else if (result.decompiled != null)
                    {
                        DecompiledEditor.Document.Text = result.decompiled;
                        vm.PopulateCurrentLocals(dataa, code);

                        RestoreCaretPosition(DecompiledEditor, currLine, currColumn, scrollPos);

                        openSaveDialog = vm.Editor.IsSaving;
                    }

                    DecompiledEditor.Document.EndUpdate();
                    vm.IsReadOnly = false;
                    if (first)
                        DecompiledEditor.Document.UndoStack.ClearAll();

                    vm.DecompiledChanged = false;

                    vm.CurrentDecompiled = code;
                    vm.loadingDialogSerivce.Hide();
                });
            });
            vm.loadingDialogSerivce.Hide();

            vm.Editor.IsSaving = false;

            if (openSaveDialog)
            {
                await vm.fileService.SaveFileAsync(TopLevel.GetTopLevel(this)!.StorageProvider);
            }
        }
    }

    private async Task DecompiledLostFocusBody(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (!vm.DecompiledFocused)
            return;
        if (vm.IsReadOnly)
            return;
        vm.DecompiledFocused = false;

        if (!vm.DecompiledChanged)
            return;

        UndertaleCode code;
        if (vm.DecompiledSkipped)
        {
            code = vm.CurrentDecompiled;
            vm.DecompiledSkipped = false;
        }
        else
            code = vm.UndertaleCode;

        if (code == null)
        {
            if (IsLoaded)
                code = vm.CurrentDecompiled; // switched to the tab with different object type
            else
                return;                   // probably loaded another data.win or something.
        }

        if (code.ParentEntry != null)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.FocusManager == null) return;

        var currentlyFocused = topLevel.FocusManager.GetFocusedElement();

        if (currentlyFocused != null && DecompiledEditor.IsVisualAncestorOf(currentlyFocused as Visual))
            return;

        UndertaleData data = AppConstants.Data;

        vm.loadingDialogSerivce.Show("Compiling", "Compiling, please wait...");

        CompileResult compileResult = new();
        string rootException = null;
        string text = DecompiledEditor.Text;
        await Task.Run(() =>
        {
            try
            {
                CompileGroup group = new(data);
                group.MainThreadAction = (f) => { Dispatcher.UIThread.Invoke(() => f()); };
                group.QueueCodeReplace(code, text);
                compileResult = group.Compile();
            }
            catch (Exception ex)
            {
                rootException = ex.ToString();
            }
        });

        if (rootException is not null)
        {
            vm.loadingDialogSerivce.Hide();
            await App.Current!.ShowError(Truncate(rootException, 512), "Compiler error");
            return;
        }

        if (!compileResult.Successful)
        {
            vm.loadingDialogSerivce.Hide();
            await App.Current!.ShowError(Truncate(compileResult.PrintAllErrors(false), 512), "Compiler error");
            return;
        }

        if (Settings.Instance.ProfileModeEnabled && AppConstants.ProfileHash is string currentMD5)
        {
            try
            {
                // Write text, only if in the profile mode.
                string path = Path.Combine(Settings.ProfilesFolder, currentMD5, "Temp", code.Name.Content + ".gml");
                File.WriteAllText(path, DecompiledEditor.Text);
            }
            catch (Exception exc)
            {
                await App.Current!.ShowError("Error during writing of GML code to profile:\n" + exc);
            }
        }

        // Invalidate gettext if necessary
        if (code.Name.Content == "gml_Script_textdata_en")
            vm.Gettext = null;

        // Show new code, decompiled.
        vm.CurrentDisassembled = null;
        vm.CurrentDecompiled = null;

        // Tab switch
        if (e == null)
        {
            vm.loadingDialogSerivce.Hide();
            return;
        }

        // Decompile new code
        await DecompileCode(code, false);
    }

    public void ChangeLineNumber(int lineNum, CodeEditorTab editorTab)
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (lineNum < 1)
            return;

        if (editorTab == CodeEditorTab.Unknown)
        {
            Debug.WriteLine($"The \"{nameof(editorTab)}\" argument of \"{nameof(ChangeLineNumber)}()\" is \"{nameof(CodeEditorTab.Unknown)}\".");
            return;
        }

        if (editorTab == CodeEditorTab.Decompiled)
            vm.OverriddenDecompPos = (lineNum, -1, -1);
        else
            vm.OverriddenDisasmPos = (lineNum, -1, -1);
    }

    private static void RestoreCaretPosition(TextEditor textEditor, int linePos, int columnPos, double scrollPos)
    {
        if (linePos <= textEditor.LineCount)
        {
            if (linePos == -1)
                linePos = textEditor.Document.LineCount;

            int lineLen = textEditor.Document.GetLineByNumber(linePos).Length;
            textEditor.TextArea.Caret.Line = linePos;
            if (columnPos != -1)
                textEditor.TextArea.Caret.Column = columnPos;
            else
                textEditor.TextArea.Caret.Column = lineLen + 1;

            textEditor.ScrollToLine(linePos);
            if (scrollPos != -1)
                textEditor.ScrollToVerticalOffset(scrollPos);
        }
        else
        {
            textEditor.CaretOffset = textEditor.Text.Length;
            textEditor.ScrollToEnd();
        }
    }

    public static void ChangeLineNumber(int lineNum, TextEditor textEditor)
    {
        if (lineNum < 1)
            return;

        if (textEditor is null)
        {
            Debug.WriteLine($"The \"{nameof(textEditor)}\" argument of \"{nameof(ChangeLineNumber)}()\" is null.");
            return;
        }

        RestoreCaretPosition(textEditor, lineNum, -1, -1);
    }

    public void RestoreState()
    {
        if (DataContext is not UndertaleCodeEditorViewModel vm)
            return;

        if (vm.IsDecompiledOpen)
            CodeModeTabs.SelectedItem = DecompiledTab;
        else
            CodeModeTabs.SelectedItem = DisassemblyTab;

        TextEditor textEditor = DecompiledEditor;
        (int linePos, int columnPos, double scrollPos) = vm.DecompiledCodePosition;
        RestoreCaretPosition(textEditor, linePos, columnPos, scrollPos);

        textEditor = DisassemblyEditor;
        (linePos, columnPos, scrollPos) = vm.DisassemblyCodePosition;
        RestoreCaretPosition(textEditor, linePos, columnPos, scrollPos);
    }

    public class NumberGenerator : VisualLineElementGenerator
    {
        private readonly IHighlighter highlighterInst;
        private readonly UndertaleCodeEditorView codeEditorInst;

        // <offset, length>
        private readonly Dictionary<int, int> lineNumberSections = new();

        public NumberGenerator(UndertaleCodeEditorView codeEditorInst, TextArea textAreaInst)
        {
            this.codeEditorInst = codeEditorInst;

            highlighterInst = textAreaInst.GetService(typeof(IHighlighter)) as IHighlighter;
        }

        public override void StartGeneration(ITextRunConstructionContext context)
        {
            lineNumberSections.Clear();

            var docLine = context.VisualLine.FirstDocumentLine;
            if (docLine.Length != 0)
            {
                int line = docLine.LineNumber;
                var highlighter = highlighterInst;

                HighlightedLine highlighted;
                try
                {
                    highlighted = highlighter.HighlightLine(line);
                }
                catch
                {
                    Debug.WriteLine($"(NumberGenerator) Code editor line {line} highlight error.");
                    base.StartGeneration(context);
                    return;
                }

                foreach (var section in highlighted.Sections)
                {
                    if (section.Color.Name == "Number")
                        lineNumberSections[section.Offset] = section.Length;
                }
            }

            base.StartGeneration(context);
        }

        /// Gets the first offset >= startOffset where the generator wants to construct
        /// an element.
        /// Return -1 to signal no interest.
        public override int GetFirstInterestedOffset(int startOffset)
        {
            foreach (var section in lineNumberSections)
            {
                if (startOffset <= section.Key)
                    return section.Key;
            }

            return -1;
        }

        /// Constructs an element at the specified offset.
        /// May return null if no element should be constructed.
        public override VisualLineElement ConstructElement(int offset)
        {
            if (codeEditorInst.DataContext is not UndertaleCodeEditorViewModel vm)
                return null;

            int numLength = -1;
            if (!lineNumberSections.TryGetValue(offset, out numLength))
                return null;

            var doc = CurrentContext.Document;
            string numText = doc.GetText(offset, numLength);

            var line = new ClickVisualLineText(numText, CurrentContext.VisualLine, numLength);

            line.Clicked += (text, inNewTab) =>
            {
                if (int.TryParse(text, out int id))
                {
                    vm.DecompiledFocused = true;
                    UndertaleData data = AppConstants.Data!;

                    List<UndertaleObject> possibleObjects = new List<UndertaleObject>();
                    if (id >= 0)
                    {
                        if (id < data.Sprites.Count)
                            possibleObjects.Add(data.Sprites[id]);
                        if (id < data.Rooms.Count)
                            possibleObjects.Add(data.Rooms[id]);
                        if (id < data.GameObjects.Count)
                            possibleObjects.Add(data.GameObjects[id]);
                        if (id < data.Backgrounds.Count)
                            possibleObjects.Add(data.Backgrounds[id]);
                        if (id < data.Scripts.Count)
                            possibleObjects.Add(data.Scripts[id]);
                        if (id < data.Paths.Count)
                            possibleObjects.Add(data.Paths[id]);
                        if (id < data.Fonts.Count)
                            possibleObjects.Add(data.Fonts[id]);
                        if (id < data.Sounds.Count)
                            possibleObjects.Add(data.Sounds[id]);
                        if (id < data.Shaders.Count)
                            possibleObjects.Add(data.Shaders[id]);
                        if (id < data.Timelines.Count)
                            possibleObjects.Add(data.Timelines[id]);
                        if (id < (data.AnimationCurves?.Count ?? 0))
                            possibleObjects.Add(data.AnimationCurves[id]);
                        if (id < (data.Sequences?.Count ?? 0))
                            possibleObjects.Add(data.Sequences[id]);
                        if (id < (data.ParticleSystems?.Count ?? 0))
                            possibleObjects.Add(data.ParticleSystems[id]);
                    }

                    MenuFlyout contextMenu = new();
                    foreach (UndertaleObject obj in possibleObjects)
                    {
                        if (obj is null)
                        {
                            continue;
                        }
                        MenuItem item = new();
                        item.Header = obj.ToString().Replace("_", "__");
                        item.PointerPressed += (sender2, ev2) =>
                        {
                            if (!ev2.Properties.IsLeftButtonPressed 
                                && !ev2.Properties.IsMiddleButtonPressed)
                                return;

                            if (ev2.Properties.IsMiddleButtonPressed)
                            {
                                vm.Editor.OpenAssetInNewTabCommand.Execute(obj);
                            }

                            else if ((ev2.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift)
                                vm.Editor.OpenAssetInTabCommand.Execute(obj);
                            else
                            {
                                doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                            text.Length, (obj as UndertaleNamedResource).Name.Content, null);
                                vm.DecompiledChanged = true;
                            }
                        };
                        contextMenu.Items.Add(item);
                    }
                    if (id > 0x00050000)
                    {
                        MenuItem item = new();
                        item.Header = "0x" + id.ToString("X6") + " (color)";
                        item.PointerPressed += (sender2, ev2) =>
                        {
                            if (ev2.GetCurrentPoint(item).Properties.IsLeftButtonPressed)
                            {
                                if (!((ev2.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift))
                                {
                                    doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                text.Length, "0x" + id.ToString("X6"), null);
                                    vm.DecompiledChanged = true;
                                }
                            }
                        };
                        contextMenu.Items.Add(item);
                    }
                    BuiltinList list = AppConstants.Data!.BuiltinList;
                    var myKey = list.Constants.FirstOrDefault(x => x.Value == (double)id).Key;
                    if (myKey != null)
                    {
                        MenuItem item = new();
                        item.Header = myKey.Replace("_", "__") + " (constant)";
                        item.PointerPressed += (sender2, ev2) =>
                        {
                            if (ev2.GetCurrentPoint(item).Properties.IsLeftButtonPressed)
                            {
                                if (!((ev2.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift))
                                {
                                    doc.Replace(line.ParentVisualLine.StartOffset + line.RelativeTextOffset,
                                                text.Length, myKey, null);
                                    vm.DecompiledChanged = true;
                                }
                            }
                        };
                        contextMenu.Items.Add(item);
                    }
                    contextMenu.Items.Add(new MenuItem() { Header = id + " (number)", IsEnabled = false });

                    contextMenu.ShowAt(codeEditorInst.DecompiledEditor);
                }
            };

            return line;
        }
    }

    public class NameGenerator : VisualLineElementGenerator
    {
        private readonly IHighlighter highlighterInst;
        private readonly TextEditor textEditorInst;
        private readonly UndertaleCodeEditorView codeEditorInst;

        private static readonly SolidColorBrush FunctionBrush = new(Color.FromRgb(0xFF, 0xB8, 0x71));
        private static readonly SolidColorBrush GlobalBrush = new(Color.FromRgb(0xF9, 0x7B, 0xF9));
        private static readonly SolidColorBrush ConstantBrush = new(Color.FromRgb(0xFF, 0x80, 0x80));
        private static readonly SolidColorBrush InstanceBrush = new(Color.FromRgb(0x58, 0xE3, 0x5A));
        private static readonly SolidColorBrush LocalBrush = new(Color.FromRgb(0xFF, 0xF8, 0x99));

        private static MenuFlyout contextMenu;
        private static object flyoutDataContext;

        // <offset, length>
        private readonly Dictionary<int, int> lineNameSections = new();

        public NameGenerator(UndertaleCodeEditorView codeEditorInst, TextArea textAreaInst)
        {
            if (codeEditorInst.DataContext is not UndertaleCodeEditorViewModel vm)
                return;

            this.codeEditorInst = codeEditorInst;

            highlighterInst = textAreaInst.GetService(typeof(IHighlighter)) as IHighlighter;
            textEditorInst = textAreaInst.GetService(typeof(TextEditor)) as TextEditor;

            var menuItem = new MenuItem()
            {
                Header = "Open in new tab"
            };
            menuItem.Click += (sender, _) =>
            {
                vm.Editor.OpenAssetInNewTabCommand.Execute(flyoutDataContext);
            };
            contextMenu = new()
            {
                Items = { menuItem },
                Placement = PlacementMode.Pointer
            };
        }

        public override void StartGeneration(ITextRunConstructionContext context)
        {
            lineNameSections.Clear();

            var docLine = context.VisualLine.FirstDocumentLine;
            if (docLine.Length != 0)
            {
                int line = docLine.LineNumber;
                var highlighter = highlighterInst;

                HighlightedLine highlighted = null;
                try
                {
                    if (highlighter != null)
                        highlighted = highlighter.HighlightLine(line);
                }
                catch
                {
                    Debug.WriteLine($"(NameGenerator) Code editor line {line} highlight error.");
                    base.StartGeneration(context);
                    return;
                }
                if (highlighted != null)
                {
                    foreach (var section in highlighted.Sections)
                    {
                        if (section.Color.Name == "Identifier" || section.Color.Name == "Function")
                            lineNameSections[section.Offset] = section.Length;
                    }
                }
            }

            base.StartGeneration(context);
        }

        /// Gets the first offset >= startOffset where the generator wants to construct
        /// an element.
        /// Return -1 to signal no interest.
        public override int GetFirstInterestedOffset(int startOffset)
        {
            foreach (var section in lineNameSections)
            {
                if (startOffset <= section.Key)
                    return section.Key;
            }

            return -1;
        }

        /// Constructs an element at the specified offset.
        /// May return null if no element should be constructed.
        public override VisualLineElement ConstructElement(int offset)
        {
            if (codeEditorInst.DataContext is not UndertaleCodeEditorViewModel vm)
                return null;

            int nameLength = -1;
            if (!lineNameSections.TryGetValue(offset, out nameLength))
                return null;

            var doc = CurrentContext.Document;
            string nameText = doc.GetText(offset, nameLength);

            UndertaleData data = AppConstants.Data;
            bool func = (offset + nameLength + 1 < CurrentContext.VisualLine.LastDocumentLine.EndOffset) &&
                        (doc.GetCharAt(offset + nameLength) == '(');
            UndertaleNamedResource val = null;
            bool nonResourceReference = false;

            var editor = textEditorInst;

            // Process the content of this identifier/function
            if (func)
            {
                val = null;
                if (!data.IsVersionAtLeast(2, 3)) // in GMS2.3 every custom "function" is in fact a member variable and scripts are never referenced directly
                    vm.ScriptsDict.TryGetValue(nameText, out val);
                if (val == null)
                {
                    vm.FunctionsDict.TryGetValue(nameText, out val);
                    if (data.IsVersionAtLeast(2, 3))
                    {
                        if (val != null)
                        {
                            if (vm.CodeDict.TryGetValue(val.Name.Content, out _))
                                val = null; // in GMS2.3 every custom "function" is in fact a member variable, and the names in functions make no sense (they have the gml_Script_ prefix)
                        }
                        else
                        {
                            // Resolve 2.3 sub-functions for their parent entry
                            if (data.GlobalFunctions?.TryGetFunction(nameText, out Underanalyzer.IGMFunction f) == true)
                            {
                                vm.ScriptsDict.TryGetValue(f.Name.Content, out val);
                                val = (val as UndertaleScript)?.Code?.ParentEntry;
                            }
                        }
                    }
                }
                if (val == null)
                {
                    if (data.BuiltinList.Functions.ContainsKey(nameText))
                    {
                        var res = new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                          FunctionBrush);
                        res.Bold = true;
                        return res;
                    }
                }
            }
            else
            {
                vm.NamedObjDict.TryGetValue(nameText, out val);
                if (data.IsVersionAtLeast(2, 3))
                {
                    if (val is UndertaleScript)
                        val = null; // in GMS2.3 scripts are never referenced directly

                    if (data.GlobalFunctions?.TryGetFunction(nameText, out Underanalyzer.IGMFunction globalFunc) == true &&
                        globalFunc is UndertaleFunction utGlobalFunc)
                    {
                        // Try getting script that this function reference belongs to
                        if (vm.NamedObjDict.TryGetValue("gml_Script_" + nameText, out val) && val is UndertaleScript script)
                        {
                            // Highlight like a function as well
                            val = script.Code;
                            func = true;
                        }
                    }

                    if (val == null)
                    {
                        // Try to get basic function
                        if (vm.FunctionsDict.TryGetValue(nameText, out val))
                        {
                            func = true;
                        }
                    }

                    if (val == null)
                    {
                        // Try resolving to room instance ID
                        string instanceIdPrefix = data.ToolInfo.InstanceIdPrefix();
                        if (nameText.StartsWith(instanceIdPrefix) &&
                            int.TryParse(nameText[instanceIdPrefix.Length..], out int id) && id >= 100000)
                        {
                            // TODO: We currently mark this as a non-resource reference, but ideally
                            // we resolve this to the room that this instance ID occurs in.
                            // However, we should only do this when actually clicking on it.
                            nonResourceReference = true;
                        }
                    }
                }
            }
            if (val == null && !nonResourceReference)
            {
                // Check for variable name colors
                if (offset >= 7)
                {
                    if (doc.GetText(offset - 7, 7) == "global.")
                    {
                        return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                       GlobalBrush);
                    }
                }
                if (data.BuiltinList.Constants.ContainsKey(nameText))
                    return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                   ConstantBrush);
                if (data.BuiltinList.GlobalNotArray.ContainsKey(nameText) ||
                    data.BuiltinList.Instance.ContainsKey(nameText) ||
                    data.BuiltinList.GlobalArray.ContainsKey(nameText))
                    return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                   InstanceBrush);
                if (vm?.CurrentLocals?.Contains(nameText) == true)
                    return new ColorVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                                   LocalBrush);
                return null;
            }

            var line = new ClickVisualLineText(nameText, CurrentContext.VisualLine, nameLength,
                                               func ? FunctionBrush : ConstantBrush);
            if (func)
            {
                // Make function references bold as well as a different color
                line.Bold = true;
            }
            if (val is not null)
            {
                // Add click operation when we have a resource
                line.Clicked += async (text, button) =>
                {
                    await codeEditorInst?.SaveChanges();

                    if (button == MouseButton.Right)
                    {
                        // Since there is no DataContext in a MenuFlyout, have to make my own.
                        flyoutDataContext = val;
                        contextMenu.ShowAt(codeEditorInst.DecompiledEditor);
                    }
                    else
                    {
                        if (button == MouseButton.Middle)
                            vm.Editor.OpenAssetInNewTabCommand.Execute(val);
                        else
                            vm.Editor.OpenAssetInTabCommand.Execute(val);
                    }
                };
            }

            return line;
        }
    }

    public class ColorVisualLineText : VisualLineText
    {
        private string Text { get; set; }
        private Brush ForegroundBrush { get; set; }

        public bool Bold { get; set; } = false;

        /// <summary>
        /// Creates a visual line text element with the specified length.
        /// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
        /// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
        /// </summary>
        public ColorVisualLineText(string text, VisualLine parentVisualLine, int length, Brush foregroundBrush)
            : base(parentVisualLine, length)
        {
            Text = text;
            ForegroundBrush = foregroundBrush;
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            if (ForegroundBrush != null)
                TextRunProperties.SetForegroundBrush(ForegroundBrush);
            if (Bold)
                TextRunProperties.SetTypeface(new Typeface(TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal));
            return base.CreateTextRun(startVisualColumn, context);
        }

        protected override VisualLineText CreateInstance(int length)
        {
            return new ColorVisualLineText(Text, ParentVisualLine, length, null);
        }
    }

    public class ClickVisualLineText : VisualLineText
    {

        public delegate void ClickHandler(string text, MouseButton button);

        public event ClickHandler Clicked;

        private string Text { get; set; }
        private Brush ForegroundBrush { get; set; }

        public bool Bold { get; set; } = false;

        /// <summary>
        /// Creates a visual line text element with the specified length.
        /// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
        /// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
        /// </summary>
        public ClickVisualLineText(string text, VisualLine parentVisualLine, int length, Brush foregroundBrush = null)
            : base(parentVisualLine, length)
        {
            Text = text;
            ForegroundBrush = foregroundBrush;
        }


        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            if (ForegroundBrush != null)
                TextRunProperties.SetForegroundBrush(ForegroundBrush);
            if (Bold)
                TextRunProperties.SetTypeface(new Typeface(TextRunProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal));
            return base.CreateTextRun(startVisualColumn, context);
        }

        bool LinkIsClickable(PointerEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
                return false;
            return (e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control;
        }


        protected override void OnQueryCursor(PointerEventArgs e)
        {
            if (LinkIsClickable(e))
            {
                e.Handled = true;
                (e.Source as Control).Cursor = new Cursor(StandardCursorType.Hand);
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.Handled)
                return;

            var properties = e.Properties;

            MouseButton button = MouseButton.None;
            if (properties.IsLeftButtonPressed)
                button = MouseButton.Left;
            else if (properties.IsMiddleButtonPressed)
                button = MouseButton.Middle;
            else if (properties.IsRightButtonPressed)
                button = MouseButton.Right;

            if (button != MouseButton.None)
            {
                // Check if the click is valid (Ctrl+Left or any Middle/Right click)
                if ((button == MouseButton.Left && LinkIsClickable(e)) || button == MouseButton.Middle || button == MouseButton.Right)
                {
                    if (Clicked != null)
                    {
                        Clicked(Text, button);
                        e.Handled = true;
                    }
                }
            }
        }

        protected override VisualLineText CreateInstance(int length)
        {
            var res = new ClickVisualLineText(Text, ParentVisualLine, length);
            res.Clicked += Clicked;
            return res;
        }
    }
}