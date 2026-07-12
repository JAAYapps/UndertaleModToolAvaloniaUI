using System;
using System.IO;

namespace UndertaleModToolAvalonia.Services.PlayerService;

public class DummyPlayer : IPlayer
{
    public DummyPlayer()
    {
        Console.WriteLine("[DummyPlayer] Audio disabled for FreeBSD compatibility.");
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