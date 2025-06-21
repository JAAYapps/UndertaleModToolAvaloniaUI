using System;
using System.IO;
using AAYInvisionaryTTSPlayer.Utilities;
using Avalonia;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using UndertaleModToolAvalonia.Services.PlayerService;

namespace UndertaleModToolAvalonia.Utilities;

public static class ScriptMessages
{
    private static IPlayer player = App.Current!.Services.GetService<IPlayer>()!;
    
    public static void ScriptMessage(string message)
    {
        _ = Application.Current!.ShowMessage(message, "Script message");
    }
    public static bool ScriptQuestion(string message)
    {
        PlayInformationSound();
        return Application.Current!.ShowQuestion(message, Icon.Question, "Script Question").Result == ButtonResult.Yes;
    }
    public static void ScriptWarning(string message)
    {
        _ = Application.Current!.ShowWarning(message, "Script warning");
    }
    public static void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        _ = Application.Current!.ShowError(error, title);
    }
    
    public static void PlayInformationSound()
    {
        try
        {
            player.Play(AssetGrabber.LoadSound());
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "\n");
            //throw;
        }
    }
}