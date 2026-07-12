using System.IO;
using System.Runtime.InteropServices.JavaScript;

namespace UndertaleModToolAvalonia.Services.PlayerService;

public partial class WebAudioPlayer : IPlayer
{
    public void Stop()
    {
        StopAllAudio();
    }

    public IPlayer.SoundStatus GetPlayStatus()
    {
        return IsAudioPlaying() ? IPlayer.SoundStatus.Playing : IPlayer.SoundStatus.Stopped;
    }

    [JSImport("playAudioFromBytes", "WebAudioPlayer")]
    private static partial void PlayAudioFromBytes(byte[] audioData);

    [JSImport("stopAllAudio", "WebAudioPlayer")]
    private static partial void StopAllAudio();

    [JSImport("isPlaying", "WebAudioPlayer")]
    [return: JSMarshalAs<JSType.Boolean>]
    private static partial bool IsAudioPlaying();
    
    public void Play(Stream audio)
    {
        // Convert the stream to a byte array to pass to JavaScript
        using var memoryStream = new MemoryStream();
        audio.CopyTo(memoryStream);
        byte[] audioBytes = memoryStream.ToArray();

        PlayAudioFromBytes(audioBytes);
    }

    public void PlayPause()
    {
        Stop();
    }

    public float Pitch { get; set; }
    public float Volume { get; set; }
    public bool IsPlaying { get; }
}