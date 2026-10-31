using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using Xunit;

namespace UndertaleModLibTests.Decompiler;

public class GameSpecificResolverTest
{
    private static GameSpecificResolver.GameSpecificDefinition UndertaleDefinition()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "GameSpecificData", "Definitions", "undertale.json");
        return JsonSerializer.Deserialize<GameSpecificResolver.GameSpecificDefinition>(File.ReadAllText(path));
    }

    private static UndertaleData Game(string displayName, params string[] codeNames)
    {
        UndertaleData data = UndertaleData.CreateNew();
        data.GeneralInfo.DisplayName = data.Strings.MakeString(displayName);
        foreach (string codeName in codeNames)
        {
            data.Code.Add(new UndertaleCode { Name = data.Strings.MakeString(codeName) });
        }
        return data;
    }

    [Fact]
    public void TestRenamedUndertaleModUsesUndertaleDefinitions()
    {
        Assert.True(UndertaleDefinition().Evaluate(Game("UNDERSWAP", "gml_Script_SCR_GAMESTART")));
    }

    [Fact]
    public void TestUndertaleStillMatchesByName()
    {
        Assert.True(UndertaleDefinition().Evaluate(Game("UNDERTALE")));
    }

    [Fact]
    public void TestOtherGameWithoutUndertaleStartScriptDoesNotMatch()
    {
        Assert.False(UndertaleDefinition().Evaluate(Game("DELTARUNE Chapter 1", "gml_Script_snd_play")));
    }
}
