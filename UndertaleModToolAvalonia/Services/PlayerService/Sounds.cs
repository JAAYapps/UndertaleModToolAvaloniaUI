using System;
using Avalonia.Platform;

namespace UndertaleModToolAvalonia.Services.PlayerService
{
    public abstract class Sounds
    {
        public static bool isSoundEnabled = false;

        public static MPVSoundPlayer notify = new MPVSoundPlayer();
        // public static SoundPlayer closeResult = new SoundPlayer(
        //     AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:JAAYTransdumper.Assets.switch-18.wav")));

        public static void Volume(long vol)
        {
            notify.Volume = vol;
        }

        public static void PlayNotification()
        {
            if (isSoundEnabled)
            {
                notify.Stop();
                // soundSwitchUncheck.Stop();
                // startup.Stop();
                // finishedTest.Stop();
                // Passed.Stop();
                // Failed.Stop();
                // closeResult.Stop();
                notify.Play("Assets/chime-notification.wav");
            }
        }
    }
}
