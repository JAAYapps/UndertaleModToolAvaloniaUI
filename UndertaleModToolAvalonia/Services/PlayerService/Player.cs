using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SFML.Audio;

namespace UndertaleModToolAvalonia.Services.PlayerService
{
    public class Player : IPlayer
    {
        private SFML.Audio.Sound sound = new SFML.Audio.Sound();

        private bool playing = false;
        
        private Stream? current;

        public void Stop()
        {
            sound.Stop();
            current?.Close();
        }

        public SFML.Audio.SoundStatus GetPlayStatus()
        {
            return sound.Status;
        }

        public void Play(Stream audio)
        {
            current = audio;
            sound.SoundBuffer = new SoundBuffer(current);
            sound.Play();
            playing = true;
        }

        public void PlayPause()
        {
            if (sound.Status != SFML.Audio.SoundStatus.Stopped)
            {
                if (playing)
                    sound.Pause();
                else
                    sound.Play();
                playing = !playing;
            }
        }

        public float Pitch
        {
            get
            {
                return sound.Pitch;
            }
            set
            {
                sound.Pitch = value;
            }
        }

        public float Volume
        {
            get
            {
                return sound.Volume;
            }
            set
            {
                sound.Volume = value;
            }
        }

        public bool IsPlaying => sound.Status == SoundStatus.Playing;
    }
}
