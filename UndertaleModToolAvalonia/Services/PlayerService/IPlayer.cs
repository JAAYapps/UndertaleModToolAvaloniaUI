using System.IO;

namespace UndertaleModToolAvalonia.Services.PlayerService
{
    internal interface IPlayer
    {
        public void Stop();

        public SFML.Audio.SoundStatus GetPlayStatus();

        public void Play(Stream audio);

        public void PlayPause();

        public float Pitch { get; set; }

        public float Volume { get; set; }
    }
}
