using System;
using System.IO;

namespace UndertaleModToolAvalonia.Services.PlayerService;

public class DummyPlayer : IPlayer
{
    public string? LoadError { get; }

    public DummyPlayer()
    {
        Console.WriteLine("[DummyPlayer] Audio disabled for FreeBSD compatibility.");
    }

    public DummyPlayer(string loadError)
    {
        LoadError = loadError;
        Console.WriteLine("[DummyPlayer] Audio disabled because the audio library failed to load: " + loadError);
    }
    
    public void Stop()
    {
    }

    public IPlayer.SoundStatus GetPlayStatus()
    {
        return IPlayer.SoundStatus.Stopped;
    }

    public void Play(Stream audio)
    {
    }

    public void PlayPause()
    {
    }

    public bool IsPlaying { get; set; } = false;
    public float Volume { get; set; } = 1.0f;
    public float Pitch { get; set; }
}