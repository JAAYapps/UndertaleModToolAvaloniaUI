using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Project;
using Xunit;

namespace UndertaleModLibTests.Project;

public class ProjectExportTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "umt_project_test_" + Guid.NewGuid().ToString("N"));

    public ProjectExportTest()
    {
        Directory.CreateDirectory(Path.Combine(_root, "game"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    private string DataPath => Path.Combine(_root, "game", "data.win");

    private string ProjectFile => Path.Combine(_root, "project", "project.json");

    [Fact]
    public void TestStreamedSoundWithOtherFilenameRoundTrips()
    {
        UndertaleData data = UndertaleData.CreateNew();
        UndertaleSound sound = new()
        {
            Name = data.Strings.MakeString("snd_test"),
            Flags = UndertaleSound.AudioEntryFlags.Regular,
            Type = data.Strings.MakeString(".ogg"),
            File = data.Strings.MakeString("other_music.ogg"),
            Volume = 1
        };
        data.Sounds.Add(sound);
        byte[] music = [1, 2, 3, 4];
        File.WriteAllBytes(Path.Combine(_root, "game", "other_music.ogg"), music);

        ProjectContext project = new(data, DataPath, DataPath, ProjectFile, "Test");
        project.MarkAssetForExport(sound);
        project.Export(true);

        Assert.True(File.Exists(Path.Combine(_root, "project", "sounds", "snd_test", "snd_test.ogg")));

        File.Delete(Path.Combine(_root, "game", "other_music.ogg"));
        UndertaleData fresh = UndertaleData.CreateNew();
        ProjectContext.CreateWithDataFilePaths(DataPath, DataPath, ProjectFile).Import(fresh, new GameFileNoOpBackup());

        Assert.Equal("other_music.ogg", fresh.Sounds.ByName("snd_test").File.Content);
        Assert.Equal(music, File.ReadAllBytes(Path.Combine(_root, "game", "other_music.ogg")));
    }

    [Fact]
    public void TestStreamedSoundSavedUnderItsFilenameStillImports()
    {
        Directory.CreateDirectory(Path.Combine(_root, "project", "sounds", "snd_test"));
        File.WriteAllText(ProjectFile, "{ \"Name\": \"Test\" }");
        File.WriteAllText(Path.Combine(_root, "project", "sounds", "snd_test", "snd_test.json"),
            "{ \"$type\": \"Sound\", \"Filename\": \"other_music.ogg\", \"Type\": \".ogg\", \"Volume\": 1, \"Streamed\": true }");
        byte[] music = [5, 6, 7];
        File.WriteAllBytes(Path.Combine(_root, "project", "sounds", "snd_test", "other_music.ogg"), music);

        UndertaleData fresh = UndertaleData.CreateNew();
        ProjectContext.CreateWithDataFilePaths(DataPath, DataPath, ProjectFile).Import(fresh, new GameFileNoOpBackup());

        Assert.Equal("other_music.ogg", fresh.Sounds.ByName("snd_test").File.Content);
        Assert.Equal(music, File.ReadAllBytes(Path.Combine(_root, "game", "other_music.ogg")));
    }

    [Theory]
    [InlineData("obj-pjum test", "obj_pjum_test")]
    [InlineData("obj.a.b", "obj_a_b")]
    [InlineData("9obj", "_obj")]
    public void TestFilenameKeepsEveryLetter(string assetName, string fileName)
    {
        UndertaleData data = UndertaleData.CreateNew();
        UndertaleGameObject obj = new() { Name = data.Strings.MakeString(assetName) };
        data.GameObjects.Add(obj);

        ProjectContext project = new(data, DataPath, DataPath, ProjectFile, "Test");
        project.MarkAssetForExport(obj);
        project.Export(true);

        Assert.True(File.Exists(Path.Combine(_root, "project", "objects", fileName + ".json")),
            "files written: " + string.Join(", ", Directory.GetFiles(Path.Combine(_root, "project", "objects")).Select(Path.GetFileName)));
    }
}
