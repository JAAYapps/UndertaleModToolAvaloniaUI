using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.LoadingDialogService;
using UndertaleModToolAvalonia.Services.ProfileService;
using UndertaleModToolAvalonia.Utilities;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleCodeEditorViewModel : EditorContentViewModel
    {
        public enum CodeEditorTab
        {
            Unknown,
            Disassembly,
            Decompiled
        }

        public CodeEditorTab EditorTab { get; set; } = CodeEditorTab.Unknown;

        public readonly IProfileService profileService;

        public readonly ILoadingDialogService loadingDialogSerivce;

        public readonly IFileService fileService;

        [ObservableProperty]
        private EditorViewModel editor;

        [ObservableProperty]
        private UndertaleCode undertaleCode;

        [ObservableProperty]
        private string decompiledText;

        [ObservableProperty]
        private string disassembledText;

        [ObservableProperty]
        private int selectedTabIndex = 0;

        [ObservableProperty]
        public UndertaleCode currentDisassembled = null;

        [ObservableProperty]
        public UndertaleCode currentDecompiled = null;

        [ObservableProperty]
        public List<string> currentLocals = new();

        [ObservableProperty]
        public bool decompiledFocused = false;

        [ObservableProperty]
        public bool decompiledChanged = false;

        [ObservableProperty]
        public bool decompiledYet = false;

        [ObservableProperty]
        public bool decompiledSkipped = false;

        [ObservableProperty]
        public (int Line, int Column, double ScrollPos) overriddenDecompPos;

        [ObservableProperty]
        public bool disassemblyFocused = false;

        [ObservableProperty]
        public bool disassemblyChanged = false;

        [ObservableProperty]
        public bool disassembledYet = false;

        [ObservableProperty]
        public bool disassemblySkipped = false;

        [ObservableProperty]
        public (int Line, int Column, double ScrollPos) overriddenDisasmPos;

        /// <summary>The decompiled code position.</summary>
        [ObservableProperty]
        public (int Line, int Column, double ScrollPos) decompiledCodePosition;

        /// <summary>The disassembly code position.</summary>
        [ObservableProperty]
        public (int Line, int Column, double ScrollPos) disassemblyCodePosition;

        /// <summary>The scroll position of decompiled code.</summary>
        [ObservableProperty]
        public double decompiledScrollPos;

        /// <summary>The scroll position of disassembly code.</summary>
        [ObservableProperty]
        public double disassemblyScrollPos;

        /// <summary>Whether the "Decompiled" tab is open.</summary>
        [ObservableProperty]
        public bool isDecompiledOpen;

        [ObservableProperty]
        private int windowHeight;

        /// <summary>Whether this state was already restored (applied to the code editor).</summary>
        public bool IsStateRestored;

        public readonly Dictionary<string, UndertaleNamedResource> NamedObjDict = new();

        public readonly Dictionary<string, UndertaleNamedResource> ScriptsDict = new();

        public readonly Dictionary<string, UndertaleNamedResource> FunctionsDict = new();

        public readonly Dictionary<string, UndertaleNamedResource> CodeDict = new();

        public Dictionary<string, string> gettextJSON = null;
        public readonly Regex gettextRegex = new(@"scr_gettext\(\""(.*?)\""\)(?!(.*?\/\/.*?$))", RegexOptions.Compiled);
        public readonly Regex getlangRegex = new(@"scr_84_get_lang_string(?:.*?)\(\""(.*?)\""\)(?!(.*?\/\/.*?$))", RegexOptions.Compiled);

        [ObservableProperty]
        public Dictionary<string, string> gettext = null;

        [ObservableProperty]
        private bool isReadOnly = true;

        public UndertaleCodeEditorViewModel(string title, UndertaleCode undertaleCode, EditorViewModel editor, IProfileService profileService, ILoadingDialogService loadingDialogSerivce, IFileService fileService) : base(title)
        {
            this.editor = editor;
            this.undertaleCode = undertaleCode;
            this.profileService = profileService;
            this.loadingDialogSerivce = loadingDialogSerivce;
            this.fileService = fileService;


        }

        // public bool IsCodeEditable => Settings.Instance.ProfileModeEnabled && UndertaleCode?.ParentEntry == null;

        [RelayCommand]
        private async Task RunCompileAsync()
        {

        }

        public async Task<(string decompiled, Exception e)> CompileAsync(UndertaleCode code, UndertaleData dataa, UndertaleCode gettextCode, string gettextJsonPath)
        {
            GlobalDecompileContext context = new(dataa);
            string decompiled = null;
            Exception e = null;
            try
            {
                if (!Settings.Instance.ProfileModeEnabled || AppConstants.ProfileHash is not string currentMD5)
                {
                    decompiled = new Underanalyzer.Decompiler.DecompileContext(context, code, dataa.ToolInfo.DecompilerSettings)
                        .DecompileToString();
                }
                else
                {
                    string path = Path.Combine(Settings.ProfilesFolder, currentMD5, "Temp", code.Name.Content + ".gml");
                    if (!File.Exists(path))
                    {
                        decompiled = new Underanalyzer.Decompiler.DecompileContext(context, code, dataa.ToolInfo.DecompilerSettings)
                            .DecompileToString();
                    }
                    else
                    {
                        decompiled = File.ReadAllText(path);
                    }
                }
            }
            catch (Exception ex)
            {
                e = ex;
            }

            if (gettextCode != null)
                await UpdateGettext(dataa, gettextCode);

            try
            {
                if (gettextJSON == null && gettextJsonPath != null && File.Exists(gettextJsonPath))
                {
                    string err = UpdateGettextJSON(File.ReadAllText(gettextJsonPath));
                    if (err != null)
                        e = new Exception(err);
                }
            }
            catch (Exception exc)
            {
                await App.Current!.ShowError(exc.ToString());
            }

            // Add `// string` at the end of lines with `scr_gettext()` or `scr_84_get_lang_string()`
            if (decompiled is not null)
            {
                StringReader decompLinesReader;
                StringBuilder decompLinesBuilder;
                Dictionary<string, string> currDict = null;
                Regex currRegex = null;
                if (Gettext is not null && decompiled.Contains("scr_gettext"))
                {
                    currDict = Gettext;
                    currRegex = gettextRegex;
                }
                else if (gettextJSON is not null && decompiled.Contains("scr_84_get_lang_string"))
                {
                    currDict = gettextJSON;
                    currRegex = getlangRegex;
                }

                if (currDict is not null && currRegex is not null)
                {
                    decompLinesReader = new(decompiled);
                    decompLinesBuilder = new();
                    string line;
                    while ((line = decompLinesReader.ReadLine()) is not null)
                    {
                        // Not `currRegex.Match()`, because one line could contain several calls
                        // if the "Profile mode" is enabled.
                        var matches = currRegex.Matches(line).Where(m => m.Success).ToArray();
                        if (matches.Length > 0)
                        {
                            decompLinesBuilder.Append($"{line} // ");

                            for (int i = 0; i < matches.Length; i++)
                            {
                                Match match = matches[i];
                                if (!currDict.TryGetValue(match.Groups[1].Value, out string text))
                                    text = "<localization fetch error>";

                                if (i != matches.Length - 1) // If not the last
                                    decompLinesBuilder.Append($"{text}; ");
                                else
                                    decompLinesBuilder.Append(text + '\n');
                            }
                        }
                        else
                        {
                            decompLinesBuilder.Append(line + '\n');
                        }
                    }

                    decompiled = decompLinesBuilder.ToString();
                }
            }

            return (decompiled, e);
        }

        public void FillObjectDicts()
        {
            var data = AppConstants.Data;
            var objLists = new IEnumerable[] {
                data.Sounds,
                data.Sprites,
                data.Backgrounds,
                data.Paths,
                data.Scripts,
                data.Fonts,
                data.GameObjects,
                data.Rooms,
                data.Extensions,
                data.Shaders,
                data.Timelines,
                data.AnimationCurves,
                data.Sequences,
                data.AudioGroups
            };

            NamedObjDict.Clear();
            ScriptsDict.Clear();
            FunctionsDict.Clear();
            CodeDict.Clear();

            foreach (var list in objLists)
            {
                if (list is null)
                    continue;

                foreach (var obj in list)
                {
                    if (obj is not UndertaleNamedResource namedObj)
                        continue;

                    NamedObjDict[namedObj.Name.Content] = namedObj;
                }
            }
            foreach (var scr in data.Scripts)
            {
                if (scr is null)
                    continue;

                ScriptsDict[scr.Name.Content] = scr;
            }
            foreach (var func in data.Functions)
            {
                if (func is null)
                    continue;

                FunctionsDict[func.Name.Content] = func;
            }
            foreach (var code in data.Code)
            {
                if (code is null)
                    continue;

                CodeDict[code.Name.Content] = code;
            }
        }

        public async Task UpdateGettext(UndertaleData data, UndertaleCode gettextCode)
        {
            gettext = new Dictionary<string, string>();
            string[] decompilationOutput;
            GlobalDecompileContext context = new(data);
            if (!Settings.Instance.ProfileModeEnabled || AppConstants.ProfileHash is not string currentMD5)
            {
                decompilationOutput = new Underanalyzer.Decompiler.DecompileContext(context, gettextCode, data.ToolInfo.DecompilerSettings)
                    .DecompileToString().Split('\n');
            }
            else
            {
                string path = Path.Combine(Settings.ProfilesFolder, currentMD5, "Temp", gettextCode.Name.Content + ".gml");
                if (File.Exists(path))
                {
                    try
                    {
                        decompilationOutput = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
                    }
                    catch
                    {
                        decompilationOutput = new Underanalyzer.Decompiler.DecompileContext(context, gettextCode, data.ToolInfo.DecompilerSettings)
                            .DecompileToString().Split('\n');
                    }
                }
                else
                {
                    decompilationOutput = new Underanalyzer.Decompiler.DecompileContext(context, gettextCode, data.ToolInfo.DecompilerSettings)
                        .DecompileToString().Split('\n');
                }
            }
            Regex textdataRegex = new("^ds_map_add\\(global\\.text_data_en, \\\"(.*)\\\", \\\"(.*)\\\"\\)", RegexOptions.Compiled);
            Regex textdataRegex2 = new("^ds_map_add\\(global\\.text_data_en, \\\"(.*)\\\", '(.*)'\\)", RegexOptions.Compiled);
            foreach (var line in decompilationOutput)
            {
                Match m = textdataRegex.Match(line);
                if (m.Success)
                {
                    try
                    {
                        if (!data.IsGameMaker2() && m.Groups[2].Value.Contains("'\"'"))
                        {
                            gettext.Add(m.Groups[1].Value, $"\"{m.Groups[2].Value}\"");
                        }
                        else
                        {
                            gettext.Add(m.Groups[1].Value, m.Groups[2].Value);
                        }
                    }
                    catch (ArgumentException)
                    {
                        await App.Current!.ShowError("There is a duplicate key in textdata_en, being " + m.Groups[1].Value + ". This may cause errors in the comment display of text.");
                    }
                    catch
                    {
                        await App.Current!.ShowError("Unknown error in textdata_en. This may cause errors in the comment display of text.");
                    }
                }
                else
                {
                    m = textdataRegex2.Match(line);
                    if (m.Success)
                    {
                        try
                        {
                            if (!data.IsGameMaker2() && m.Groups[2].Value.Contains("\"'\""))
                            {
                                gettext.Add(m.Groups[1].Value, $"\'{m.Groups[2].Value}\'");
                            }
                            else
                            {
                                gettext.Add(m.Groups[1].Value, m.Groups[2].Value);
                            }
                        }
                        catch (ArgumentException)
                        {
                            await App.Current!.ShowError("There is a duplicate key in textdata_en, being " + m.Groups[1].Value + ". This may cause errors in the comment display of text.");
                        }
                        catch
                        {
                            await App.Current!.ShowError("Unknown error in textdata_en. This may cause errors in the comment display of text.");
                        }
                    }
                }
            }
        }

        public string UpdateGettextJSON(string json)
        {
            try
            {
                gettextJSON = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception e)
            {
                gettextJSON = new Dictionary<string, string>();
                return "Failed to parse language file: " + e.Message;
            }
            return null;
        }

        public void PopulateCurrentLocals(UndertaleData data, UndertaleCode code)
        {
            CurrentLocals.Clear();

            // Look up locals for given code entry's name, for syntax highlighting
            var locals = data.CodeLocals?.ByName(code.Name.Content);
            if (locals != null)
            {
                foreach (var local in locals.Locals)
                    CurrentLocals.Add(local.Name.Content);
            }
        }

        public static string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }
}
