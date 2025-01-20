using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Threading;
using LibMpv.Client;
using LibMpv.MVVM;
using SFML.Audio;

namespace UndertaleModToolAvalonia.Services.PlayerService;

public class MPVSoundPlayer : BaseMpvContextViewModel
{
    public override void InvokeInUIThread(Action action)
    {
        Dispatcher.UIThread.Invoke(action);
    }

    protected Stream GetAssemblyResource(string name)
    {
        return AssetLoader.Open(new Uri(name));
    }

    public void Play(string filename)
    {
        LoadFile(filename);
        Play();
    }
}