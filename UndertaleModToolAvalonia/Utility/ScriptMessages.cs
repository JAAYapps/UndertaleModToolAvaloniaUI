using System;
using System.IO;
using Avalonia;
using Avalonia.Platform;
using MsBox.Avalonia.Enums;
using UndertaleModToolAvalonia.Services.PlayerService;

namespace UndertaleModToolAvalonia.Utility;

public static class ScriptMessages
{
    private static MPVSoundPlayer player = new MPVSoundPlayer();
    
    public static void ScriptMessage(string message)
    {
        Application.Current.ShowMessage(message, "Script message");
    }
    public static bool ScriptQuestion(string message)
    {
        PlayInformationSound();
        return Application.Current.ShowQuestion(message, Icon.Question, "Script Question").Result == ButtonResult.Yes;
    }
    public static void ScriptWarning(string message)
    {
        Application.Current.ShowWarning(message, "Script warning");
    }
    public static void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        Application.Current.ShowError(error, title);
    }
    
    public static void PlayInformationSound()
    {
        try
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                player.Play(@"C:\Windows\Media\Windows Exclamation.wav");
            else
                player.Play("Assets/chime-notification.wav"); // Chime Notification by Jofae -- https://freesound.org/s/380482/ -- License: Creative Commons 0
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "\n");
            //throw;
        }
    }
}