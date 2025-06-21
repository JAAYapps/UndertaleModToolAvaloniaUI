using System;
using System.IO;
using System.Reflection;
using SkiaSharp;

namespace AAYInvisionaryTTSPlayer.Utilities;

public static class AssetGrabber
{
    public static Stream GetAssetStream(string filename)
    {
        var info = Assembly.GetExecutingAssembly().GetName();  
        var name = info.Name;  
        return Assembly.GetExecutingAssembly().GetManifestResourceStream($"{name}.Assets.{filename}")!;
    }
    
    // Helper methods (placeholders)
    public static SKBitmap LoadImage(string assetPath ,string name)
    {
        try
        {
            return SKBitmap.Decode(GetAssetStream($"{assetPath}.{name}")); // Adjust path as needed
        }
        catch (Exception)
        {
            //Console.WriteLine(e);
            return SKBitmap.Decode(GetAssetStream($"Assets.icon.ico")); // Adjust path as needed
        }
    }

    public static Stream LoadSound(string assetPath = "", string name = "chime-notification.wav") // Chime Notification by Jofae -- https://freesound.org/s/380482/ -- License: Creative Commons 0
    {
        try
        {
            var path = assetPath != String.Empty ? $"{assetPath}." : "";
            return GetAssetStream($"{path}{name}"); // Adjust path as needed
        }
        catch (Exception e)
        {
            Console.WriteLine(e + "\n No Sound Found");
            return Stream.Null;
        }
    }
}