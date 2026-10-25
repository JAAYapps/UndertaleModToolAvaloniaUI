using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Project;

namespace PjumModLib;

public sealed record ProjectConvertResult(IReadOnlyDictionary<string, int> WrittenByFolder, IReadOnlyList<string> Notes);

public static class ProjectConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static ProjectConvertResult Convert(UndertaleData original, string originalPath, UndertaleData modded, string moddedPath,
                                               string projectDirectory, string projectName, Action<Action>? mainThreadAction = null)
    {
        if (Directory.Exists(projectDirectory) && Directory.EnumerateFileSystemEntries(projectDirectory).Any())
            throw new IOException($"The folder \"{projectDirectory}\" isn't empty. Choose an empty folder for the project.");

        string temp = Path.Combine(Path.GetTempPath(), "pjum_project_" + Guid.NewGuid().ToString("N"));
        try
        {
            string before = ExportAll(original, originalPath, Path.Combine(temp, "original"), mainThreadAction, []);
            var unsaveable = new List<string>();
            string after = ExportAll(modded, moddedPath, Path.Combine(temp, "modded"), mainThreadAction, unsaveable);
            Dictionary<(string Folder, string Unit), List<string>> beforeUnits = Units(before), afterUnits = Units(after);

            _ = new ProjectContext(modded, moddedPath, moddedPath, Path.Combine(projectDirectory, "project.json"), projectName, mainThreadAction);

            var written = new Dictionary<string, int>();
            var notes = unsaveable.ToList();
            var streamedFiles = new Dictionary<string, string>();
            var sharedFiles = new List<string>();
            foreach (((string folder, string unit), List<string> files) in afterUnits)
            {
                if (beforeUnits.TryGetValue((folder, unit), out List<string>? beforeFiles) && SameFiles(before, beforeFiles, after, files))
                    continue;
                if (folder == "sounds" && StreamedFile(after, unit) is string streamed)
                {
                    if (streamedFiles.ContainsKey(streamed))
                    {
                        sharedFiles.Add(unit);
                        continue;
                    }
                    streamedFiles[streamed] = unit;
                }
                foreach (string file in files)
                {
                    string destination = Path.Combine(projectDirectory, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(Path.Combine(after, file), destination);
                }
                written[folder] = written.GetValueOrDefault(folder) + 1;
                if (folder == "sounds")
                    NameMusicAfterSound(projectDirectory, unit);
            }

            WriteGameInfo(original, modded, projectDirectory, written);
            var renames = Renames(original, modded);
            WriteScripts(original, modded, projectDirectory, renames, sharedFiles, written);
            Notes(original, modded, renames, notes);
            return new ProjectConvertResult(written, notes);
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    private static string ExportAll(UndertaleData data, string dataPath, string directory, Action<Action>? mainThreadAction, List<string> unsaveable)
    {
        var context = new ProjectContext(data, dataPath, dataPath, Path.Combine(directory, "project.json"), "export", mainThreadAction);
        foreach (IList<UndertaleNamedResource> list in Lists(data).Select(l => l.List))
            foreach (UndertaleNamedResource asset in list)
            {
                if (asset is UndertaleSprite sprite && sprite.CollisionMasks.Count > 0 && sprite.CalculateMaskDimensions(data) is var (width, height) && (width <= 0 || height <= 0))
                {
                    var frame = sprite.Textures.FirstOrDefault()?.Texture;
                    unsaveable.Add($"Sprite '{sprite.Name?.Content}' is {sprite.Width}x{sprite.Height} in the modded file{(frame is null ? "" : $" (its frames are {frame.BoundingWidth}x{frame.BoundingHeight})")} but has a collision mask, so the project system can't save it. It's created empty at its right number. Fix its size in the mod tool, then convert again.");
                    continue;
                }
                var pictures = asset switch
                {
                    UndertaleSprite s => s.Textures.Select(t => t?.Texture),
                    UndertaleBackground b => [b.Texture],
                    UndertaleFont f => [f.Texture],
                    _ => Enumerable.Empty<UndertaleTexturePageItem?>()
                };
                if (pictures.FirstOrDefault(t => t is not null && (t.TargetWidth > t.BoundingWidth || t.TargetHeight > t.BoundingHeight)) is UndertaleTexturePageItem oversized)
                {
                    unsaveable.Add($"'{asset.Name?.Content}' has a picture that is bigger than its size ({oversized.TargetWidth}x{oversized.TargetHeight} inside {oversized.BoundingWidth}x{oversized.BoundingHeight}), so the project system can't save it; it wasn't converted. Re-import that picture in the mod tool, then convert again.");
                    continue;
                }
                if (asset is UndertaleSound { Flags: UndertaleSound.AudioEntryFlags.Regular, File.Content: string file } sound)
                {
                    string external = file.Contains('.') ? file : file + ".ogg";
                    if (!File.Exists(Path.Combine(Path.GetFullPath(Path.GetDirectoryName(dataPath)!), external)))
                    {
                        unsaveable.Add($"Sound '{sound.Name?.Content}' plays the file '{external}', which isn't in the modded game's folder either. It's created without its music at its right number, like in the modded game. Put the file next to the data file, then convert again.");
                        continue;
                    }
                }
                if (asset is IProjectAsset projectAsset)
                    context.MarkAssetForExport(projectAsset);
            }
        try
        {
            context.Export(true);
        }
        catch (Exception ex) when (ex is ProjectException or InvalidDataException or ArgumentException)
        {
            throw new InvalidDataException($"The mod tool's project system couldn't save part of {Path.GetFileName(Path.GetDirectoryName(dataPath))}/{Path.GetFileName(dataPath)}: {ex.Message}", ex);
        }
        File.Delete(Path.Combine(directory, "project.json"));
        return directory;
    }

    private static IEnumerable<(string Folder, string Property, string Type, IList<UndertaleNamedResource> List)> Lists(UndertaleData data)
    {
        IList<UndertaleNamedResource> Of<T>(IList<T>? list) where T : UndertaleNamedResource =>
            list is null ? [] : list.Cast<UndertaleNamedResource>().ToList();
        yield return ("sprites", "Sprites", "UndertaleSprite", Of(data.Sprites));
        yield return ("backgrounds", "Backgrounds", "UndertaleBackground", Of(data.Backgrounds));
        yield return ("fonts", "Fonts", "UndertaleFont", Of(data.Fonts));
        yield return ("sounds", "Sounds", "UndertaleSound", Of(data.Sounds));
        yield return ("objects", "GameObjects", "UndertaleGameObject", Of(data.GameObjects));
        yield return ("paths", "Paths", "UndertalePath", Of(data.Paths));
        yield return ("rooms", "Rooms", "UndertaleRoom", Of(data.Rooms));
        yield return ("scripts", "Scripts", "UndertaleScript", Of(data.Scripts));
        yield return ("code", "Code", "UndertaleCode", Of(data.Code));
        yield return ("shaders", "Shaders", "UndertaleShader", Of(data.Shaders));
        yield return ("sequences", "Sequences", "UndertaleSequence", Of(data.Sequences));
        yield return ("animcurves", "AnimationCurves", "UndertaleAnimationCurve", Of(data.AnimationCurves));
    }

    private static List<(string Folder, string Property, string Was, string Now)> Renames(UndertaleData original, UndertaleData modded)
    {
        var renames = new List<(string Folder, string Property, string Was, string Now)>();
        foreach (((string folder, string property, _, IList<UndertaleNamedResource> before), (_, _, _, IList<UndertaleNamedResource> after)) in Lists(original).Zip(Lists(modded)))
        {
            if (folder == "code")
                continue;
            var beforeNames = before.Select(a => a?.Name?.Content).OfType<string>().ToHashSet();
            var afterNames = after.Select(a => a?.Name?.Content).OfType<string>().ToHashSet();
            for (int i = 0; i < Math.Min(before.Count, after.Count); i++)
                if (before[i]?.Name?.Content is string was && after[i]?.Name?.Content is string now && was != now && now.Length > 0
                    && !afterNames.Contains(was) && !beforeNames.Contains(now))
                    renames.Add((folder, property, was, now));
        }
        foreach (var rename in renames.Where(r => r.Folder == "objects").ToList())
            foreach (UndertaleCode code in original.Code)
                if (code?.Name?.Content is string name && name.StartsWith($"gml_Object_{rename.Was}_", StringComparison.Ordinal)
                    && $"gml_Object_{rename.Now}_" + name[$"gml_Object_{rename.Was}_".Length..] is string renamed
                    && modded.Code.ByName(renamed) is not null && modded.Code.ByName(name) is null)
                    renames.Add(("code", "Code", name, renamed));
        return renames;
    }

    private static string Quote(string text) => "@\"" + text.Replace("\"", "\"\"") + "\"";

    private static void WriteScripts(UndertaleData original, UndertaleData modded, string projectDirectory, List<(string Folder, string Property, string Was, string Now)> renames,
                                     List<string> sharedFiles, Dictionary<string, int> written)
    {
        string projectFile = Path.Combine(projectDirectory, "project.json");
        JsonNode project = JsonNode.Parse(File.ReadAllText(projectFile))!;
        var lines = renames.Select(r => $"Data.{r.Property}.ByName({Quote(r.Was)}).Name = Data.Strings.MakeString({Quote(r.Now)});").ToList();
        int added = 0;
        foreach (((string folder, string property, string type, IList<UndertaleNamedResource> before), (_, _, _, IList<UndertaleNamedResource> after)) in Lists(original).Zip(Lists(modded)))
        {
            if (folder == "code")
                continue;
            var known = before.Select(a => a?.Name?.Content).OfType<string>().ToHashSet();
            known.UnionWith(renames.Where(r => r.Folder == folder).Select(r => r.Now));
            foreach (UndertaleNamedResource asset in after)
            {
                if (asset?.Name?.Content is not string name || known.Contains(name))
                    continue;
                string extra = asset is UndertaleSound { Flags: UndertaleSound.AudioEntryFlags.Regular } sound
                    ? $", Flags = UndertaleSound.AudioEntryFlags.Regular, File = Data.Strings.MakeString({Quote(sound.File?.Content ?? "")}), Type = Data.Strings.MakeString({Quote(sound.Type?.Content ?? ".ogg")}), Volume = {sound.Volume}f"
                    : "";
                lines.Add($"Data.{property}.Add(new {type} {{ Name = Data.Strings.MakeString({Quote(name)}){extra} }});");
                added++;
            }
        }
        if (lines.Count > 0)
        {
            File.WriteAllLines(Path.Combine(projectDirectory, "pjum-asset-numbers.csx"), lines);
            project["PreAssetImportScripts"] = new JsonArray("pjum-asset-numbers.csx");
            if (renames.Count > 0)
                written["renames"] = renames.Count;
            if (added > 0)
                written["new assets in order"] = added;
        }
        var afterLines = new List<string>();
        var order = modded.GeneralInfo.RoomOrder.Select(r => r.Resource?.Name?.Content).OfType<string>().ToList();
        if (!original.GeneralInfo.RoomOrder.Select(r => r.Resource?.Name?.Content).SequenceEqual(order))
        {
            afterLines.Add("Data.GeneralInfo.RoomOrder.Clear();");
            afterLines.AddRange(order.Select(room => $"{{ if (Data.Rooms.ByName({Quote(room)}) is UndertaleRoom room) Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>(room)); }}"));
            written["room order"] = 1;
        }
        foreach (string name in sharedFiles)
        {
            if (modded.Sounds.ByName(name) is not UndertaleSound sound)
                continue;
            afterLines.Add($"{{ UndertaleSound sound = Data.Sounds.ByName({Quote(name)}); sound.Flags = UndertaleSound.AudioEntryFlags.Regular; sound.AudioFile = null; " +
                           $"sound.File = Data.Strings.MakeString({Quote(sound.File?.Content ?? "")}); sound.Type = Data.Strings.MakeString({Quote(sound.Type?.Content ?? ".ogg")}); " +
                           $"sound.Volume = {sound.Volume}f; sound.Pitch = {sound.Pitch}f; }}");
            written["sounds sharing a music file"] = written.GetValueOrDefault("sounds sharing a music file") + 1;
        }
        if (afterLines.Count > 0)
        {
            File.WriteAllLines(Path.Combine(projectDirectory, "pjum-after-import.csx"), afterLines);
            project["PostImportScripts"] = new JsonArray("pjum-after-import.csx");
        }
        File.WriteAllText(projectFile, project.ToJsonString(JsonOptions));
    }

    private static Dictionary<(string Folder, string Unit), List<string>> Units(string root)
    {
        var units = new Dictionary<(string, string), List<string>>();
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file);
            string[] parts = relative.Split(Path.DirectorySeparatorChar);
            if (parts.Length < 2)
                continue;
            string unit = parts.Length == 2 ? Path.GetFileNameWithoutExtension(parts[1]) : parts[1];
            if (!units.TryGetValue((parts[0], unit), out List<string>? files))
                units[(parts[0], unit)] = files = new List<string>();
            files.Add(relative);
        }
        return units;
    }

    private static string? StreamedFile(string root, string unit)
    {
        string json = Path.Combine(root, "sounds", unit, unit + ".json");
        if (!File.Exists(json) || JsonNode.Parse(File.ReadAllText(json)) is not JsonNode node || node["Streamed"]?.GetValue<bool>() != true)
            return null;
        string? file = node["Filename"]?.GetValue<string>();
        return file is null ? null : file.Contains('.') ? file : file + ".ogg";
    }

    private static void NameMusicAfterSound(string projectDirectory, string unit)
    {
        if (StreamedFile(projectDirectory, unit) is not string streamed)
            return;
        string directory = Path.Combine(projectDirectory, "sounds", unit);
        string copied = Path.Combine(directory, Path.GetFileName(streamed));
        string expected = Path.Combine(directory, unit + Path.GetExtension(streamed));
        if (copied != expected && File.Exists(copied) && !File.Exists(expected))
            File.Move(copied, expected);
    }

    private static bool SameFiles(string beforeRoot, List<string> beforeFiles, string afterRoot, List<string> afterFiles)
    {
        if (beforeFiles.Count != afterFiles.Count || !beforeFiles.Order().SequenceEqual(afterFiles.Order()))
            return false;
        foreach (string file in afterFiles)
        {
            var a = new FileInfo(Path.Combine(beforeRoot, file));
            var b = new FileInfo(Path.Combine(afterRoot, file));
            if (a.Length != b.Length || !File.ReadAllBytes(a.FullName).AsSpan().SequenceEqual(File.ReadAllBytes(b.FullName)))
                return false;
        }
        return true;
    }

    private static void WriteGameInfo(UndertaleData original, UndertaleData modded, string projectDirectory, Dictionary<string, int> written)
    {
        UndertaleGeneralInfo was = original.GeneralInfo, now = modded.GeneralInfo;
        var info = new JsonObject();
        void Text(string key, UndertaleString? before, UndertaleString? after)
        {
            if (after?.Content is string value && value != before?.Content)
                info[key] = value;
        }
        Text("FileName", was.FileName, now.FileName);
        Text("Config", was.Config, now.Config);
        Text("Name", was.Name, now.Name);
        Text("DisplayName", was.DisplayName, now.DisplayName);
        if (was.GameID != now.GameID) info["GameID"] = now.GameID;
        if (was.DefaultWindowWidth != now.DefaultWindowWidth) info["DefaultWindowWidth"] = now.DefaultWindowWidth;
        if (was.DefaultWindowHeight != now.DefaultWindowHeight) info["DefaultWindowHeight"] = now.DefaultWindowHeight;
        if (was.SteamAppID != now.SteamAppID) info["SteamAppID"] = now.SteamAppID;
        if (info.Count == 0)
            return;
        File.WriteAllText(Path.Combine(projectDirectory, "general-info-options.json"), info.ToJsonString(JsonOptions));
        written["game info"] = 1;
    }

    private static void Notes(UndertaleData original, UndertaleData modded, List<(string Folder, string Property, string Was, string Now)> renames, List<string> notes)
    {
        foreach (((string folder, _, _, IList<UndertaleNamedResource> before), (_, _, _, IList<UndertaleNamedResource> after)) in Lists(original).Zip(Lists(modded)))
        {
            var renamedAway = renames.Where(r => r.Folder == folder).Select(r => r.Was).ToHashSet();
            var afterIndex = new Dictionary<string, int>();
            for (int i = 0; i < after.Count; i++)
                if (after[i]?.Name?.Content is string name)
                    afterIndex.TryAdd(name, i);
            var removed = new List<string>();
            var moved = new List<string>();
            for (int i = 0; i < before.Count; i++)
            {
                if (before[i]?.Name?.Content is not string name)
                    continue;
                if (renamedAway.Contains(name))
                    continue;
                if (!afterIndex.TryGetValue(name, out int now))
                    removed.Add($"'{name}'");
                else if (now != i && folder != "code")
                    moved.Add($"'{name}' {i} -> {now}");
            }
            if (removed.Count > 0)
                notes.Add($"{removed.Count} {folder} were removed in the modded file; a project can't remove assets: {List(removed)}.");
            if (moved.Count > 0)
                notes.Add($"{moved.Count} {folder} are at different numbers in the modded file; a project keeps the original numbers: {List(moved)}.");
        }

        string Timelines(UndertaleData data) => string.Join("|", (data.Timelines ?? []).Select(t =>
            $"{t?.Name?.Content}:{string.Join(",", t?.Moments.Select(m => $"{m.Step}={string.Join("+", m.Event.Select(a => a.CodeId?.Name?.Content))}") ?? [])}"));
        if (Timelines(original) != Timelines(modded))
            notes.Add("The modded file changes timelines; a project can't hold timelines.");
        string Inits(IEnumerable<UndertaleGlobalInit>? list) => string.Join("|", (list ?? []).Select(g => g?.Code?.Name?.Content));
        if (Inits(original.GlobalInitScripts) != Inits(modded.GlobalInitScripts) || Inits(original.GameEndScripts) != Inits(modded.GameEndScripts))
            notes.Add("The modded file changes which code runs when the game starts or ends (global init or game end scripts); a project can't carry that list.");
    }

    private static string List(List<string> items) =>
        string.Join(", ", items.Take(10)) + (items.Count > 10 ? $" and {items.Count - 10} more" : "");
}
