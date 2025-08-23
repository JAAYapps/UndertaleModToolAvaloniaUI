using NAudio.Wave;
using NVorbis;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static UndertaleModToolAvalonia.Services.PlayerService.IPlayer;

namespace UndertaleModToolAvalonia.Services.PlayerService
{
    public class SilkNetPlayer : IPlayer, IDisposable
    {
        private readonly AL _al;
        private readonly ALContext _alContext;

        private readonly unsafe Device* _device;
        private readonly unsafe Context* _context;

        private readonly List<(uint Source, uint Buffer)> _activeSources = new();

        private float _volume = 100f;
        private float _pitch = 1f;

        public unsafe SilkNetPlayer()
        {
            _al = AL.GetApi();
            _alContext = ALContext.GetApi();

            _device = _alContext.OpenDevice("");
            if (_device == null)
            {
                throw new Exception("Could not open an audio device.");
            }

            _context = _alContext.CreateContext(_device, null);
            _alContext.MakeContextCurrent(_context);
        }

        public float Volume { get => _volume; set => _volume = value; }
        public float Pitch { get => _pitch; set => _pitch = value; }

        public bool IsPlaying => GetPlayStatus() == SoundStatus.Playing;

        public SoundStatus GetPlayStatus()
        {
            _activeSources.RemoveAll(item =>
            {
                _al.GetSourceProperty(item.Source, GetSourceInteger.SourceState, out int state);
                if ((SourceState)state != SourceState.Playing)
                {
                    _al.DeleteSource(item.Source);
                    _al.DeleteBuffer(item.Buffer);
                    return true;
                }
                return false;
            });

            return _activeSources.Any() ? SoundStatus.Playing : SoundStatus.Stopped;
        }

        // This record will hold the decoded audio data in a standard format.
        private record PcmSound(short[] PcmData, BufferFormat Format, int SampleRate);

        public void Play(Stream audio)
        {
            PcmSound? soundData = null;

            // Read the first few bytes to identify the format
            byte[] header = new byte[4];
            audio.Read(header, 0, 4);
            audio.Seek(0, SeekOrigin.Begin); // Reset stream position

            string headerString = Encoding.ASCII.GetString(header);

            if (headerString.StartsWith("RIFF"))
            {
                soundData = DecodeWav(audio);
            }
            else if (headerString.StartsWith("OggS"))
            {
                soundData = DecodeOgg(audio);
            }
            else if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0 || headerString.StartsWith("ID3")) // MP3 check
            {
                soundData = DecodeMp3(audio);
            }
            else
            {
                throw new NotSupportedException("Unsupported audio format.");
            }

            if (soundData == null) return;

            uint buffer = _al.GenBuffer();
            uint source = _al.GenSource();

            _al.BufferData(buffer, soundData.Format, soundData.PcmData, soundData.SampleRate);

            _al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);
            _al.SetSourceProperty(source, SourceFloat.Gain, Volume / 100f);
            _al.SetSourceProperty(source, SourceFloat.Pitch, Pitch);

            _al.SourcePlay(source);

            _activeSources.Add((source, buffer));
        }

        private PcmSound DecodeOgg(Stream stream)
        {
            using var vorbis = new VorbisReader(stream, true);
            var channels = vorbis.Channels;
            var sampleRate = vorbis.SampleRate;
            BufferFormat format = channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;

            float[] sampleBuffer = new float[vorbis.TotalSamples * channels];
            vorbis.ReadSamples(sampleBuffer, 0, sampleBuffer.Length);

            short[] pcmBuffer = new short[sampleBuffer.Length];
            for (int i = 0; i < sampleBuffer.Length; i++)
            {
                pcmBuffer[i] = (short)(sampleBuffer[i] * short.MaxValue);
            }

            return new PcmSound(pcmBuffer, format, sampleRate);
        }

        private PcmSound DecodeWav(Stream stream)
        {
            using var waveReader = new WaveFileReader(stream);
            return DecodeFromWaveStream(waveReader);
        }

        private PcmSound DecodeMp3(Stream stream)
        {
            using var mp3Reader = new Mp3FileReader(stream);
            return DecodeFromWaveStream(mp3Reader);
        }

        private PcmSound DecodeFromWaveStream(WaveStream waveStream)
        {
            var sampleProvider = waveStream.ToSampleProvider();

            var channels = sampleProvider.WaveFormat.Channels;
            var sampleRate = sampleProvider.WaveFormat.SampleRate;
            BufferFormat format = channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;

            var allSamples = new List<float>();
            var readBuffer = new float[sampleRate * channels]; // A buffer to hold 1 second of audio
            int samplesRead;
            while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                allSamples.AddRange(readBuffer.Take(samplesRead));
            }

            var floatBuffer = allSamples.ToArray();

            // Convert to 16-bit PCM
            var pcmBuffer = new short[floatBuffer.Length];
            for (int i = 0; i < floatBuffer.Length; i++)
            {
                pcmBuffer[i] = (short)(floatBuffer[i] * short.MaxValue);
            }

            return new PcmSound(pcmBuffer, format, sampleRate);
        }

        public void PlayPause()
        {
            Stop();
        }

        public void Stop()
        {
            foreach (var (source, buffer) in _activeSources)
            {
                _al.SourceStop(source);
                _al.DeleteSource(source);
                _al.DeleteBuffer(buffer);
            }
            _activeSources.Clear();
        }

        public unsafe void Dispose()
        {
            Stop();
            _alContext.DestroyContext(_context);
            _alContext.CloseDevice(_device);
            _al.Dispose();
            _alContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
