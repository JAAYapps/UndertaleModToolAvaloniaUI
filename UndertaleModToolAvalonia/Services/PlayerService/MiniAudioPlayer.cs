using NAudio.Wave;
using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static UndertaleModToolAvalonia.Services.PlayerService.IPlayer;

namespace UndertaleModToolAvalonia.Services.PlayerService
{
    public unsafe partial class MiniAudioPlayer : IPlayer, IDisposable
    {
        // ==========================================
        // Native Interface (Links to your .a file)
        // ==========================================
        const string LibName = "AudioLib"; // Must match DirectPInvoke in .csproj

        [LibraryImport(LibName, EntryPoint = "InitAudio")]
        private static partial int InitAudio();

        [LibraryImport(LibName, EntryPoint = "PlayRawAudio")]
        private static partial int PlayRawAudio(float* data, ulong frameCount, uint channels, uint sampleRate);

        [LibraryImport(LibName, EntryPoint = "PauseAudio")]
        private static partial void PauseAudioNative();

        [LibraryImport(LibName, EntryPoint = "ResumeAudio")]
        private static partial void ResumeAudioNative();

        [LibraryImport(LibName, EntryPoint = "SetMasterVolume")]
        private static partial void SetMasterVolume(float volume);

        [LibraryImport(LibName, EntryPoint = "SetPitch")]
        private static partial void SetPitch(float pitch);

        [LibraryImport(LibName, EntryPoint = "StopAudio")]
        private static partial void StopAudioNative();
        
        // ==========================================
        // State Management
        // ==========================================
        
        // We hold the audio data here so GC doesn't eat it
        private float[]? _currentAudioData; 
        private GCHandle _pinnedHandle;
        
        private float _volume = 100f;
        private float _pitch = 1f;
        private bool _isPlaying = false;

        public MiniAudioPlayer()
        {
            // Initialize the C engine once
            if (InitAudio() != 0)
            {
                throw new Exception("Failed to initialize Miniaudio engine.");
            }
        }

        // Miniaudio uses 0.0f - 1.0f. Your UI likely uses 0-100.
        public float Volume 
        { 
            get => _volume; 
            set 
            {
                _volume = value;
                SetMasterVolume(value / 100f); 
            }
        }

        public float Pitch 
        { 
            get => _pitch; 
            set 
            {
                _pitch = value;
                SetPitch(value);
            }
        }

        public bool IsPlaying => _isPlaying;

        public SoundStatus GetPlayStatus()
        {
            // Simplified: If we started playing, assume we are playing.
            // For a real check, you'd need a helper in C: bool IsSoundPlaying()
            return _isPlaying ? SoundStatus.Playing : SoundStatus.Stopped;
        }

        // Simple record for floats (No buffer format needed, Miniaudio handles channel count)
        private record FloatSound(float[] Data, int Channels, int SampleRate);

        public void Play(Stream audio)
        {
            FloatSound? soundData = null;

            // 1. Detect Format (Same logic as before)
            byte[] header = new byte[4];
            audio.Read(header, 0, 4);
            audio.Seek(0, SeekOrigin.Begin);
            string headerString = Encoding.ASCII.GetString(header);

            try 
            {
                if (headerString.StartsWith("RIFF"))
                    soundData = DecodeWav(audio);
                else if (headerString.StartsWith("OggS"))
                    soundData = DecodeOgg(audio);
                else if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0 || headerString.StartsWith("ID3"))
                    soundData = DecodeMp3(audio);
                else
                    throw new NotSupportedException("Unsupported audio format.");

                if (soundData == null) return;

                // 2. Clean up previous sound
                Stop(); 

                // 3. Pin Memory
                _currentAudioData = soundData.Data;
                _pinnedHandle = GCHandle.Alloc(_currentAudioData, GCHandleType.Pinned);

                // 4. Send to C Engine
                ulong frameCount = (ulong)(_currentAudioData.Length / soundData.Channels);
                
                PlayRawAudio(
                    (float*)_pinnedHandle.AddrOfPinnedObject(), 
                    frameCount, 
                    (uint)soundData.Channels, 
                    (uint)soundData.SampleRate
                );

                // Apply current settings
                SetMasterVolume(Volume / 100f);
                SetPitch(Pitch);

                _isPlaying = true;
            }
            catch (Exception ex)
            {
                // Fallback logging
                Console.WriteLine($"Audio Error: {ex.Message}");
                Stop();
            }
        }

        // ==========================================
        // Decoders (Simpler now: No Float->Short conversion!)
        // ==========================================

        private FloatSound DecodeOgg(Stream stream)
        {
            // NVorbis supports Streams directly!
            using var vorbis = new VorbisReader(stream, true);
            
            float[] buffer = new float[vorbis.TotalSamples * vorbis.Channels];
            vorbis.ReadSamples(buffer, 0, buffer.Length);

            return new FloatSound(buffer, vorbis.Channels, vorbis.SampleRate);
        }

        private FloatSound DecodeWav(Stream stream)
        {
            using var reader = new WaveFileReader(stream);
            return DecodeSampleProvider(reader.ToSampleProvider());
        }

        private FloatSound DecodeMp3(Stream stream)
        {
            // Note: On Linux, Mp3FileReader might fail if it relies on Windows Codecs.
            // If you have NLayer or similar bundled, use that instead.
            using var reader = new Mp3FileReader(stream);
            return DecodeSampleProvider(reader.ToSampleProvider());
        }

        private FloatSound DecodeSampleProvider(ISampleProvider provider)
        {
            var pcmData = new List<float>();
            var buffer = new float[provider.WaveFormat.SampleRate * provider.WaveFormat.Channels];
            int read;

            while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                pcmData.AddRange(buffer.Take(read));
            }

            return new FloatSound(pcmData.ToArray(), provider.WaveFormat.Channels, provider.WaveFormat.SampleRate);
        }

        private bool _isPaused = false;

        public void Pause()
        {
            PauseAudioNative();
            _isPaused = true;
        }

        public void Resume()
        {
            ResumeAudioNative();
            _isPaused = false;
        }

        public void PlayPause()
        {
            if (_currentAudioData == null) return;

            // Check if the sound is actually running
            var status = GetPlayStatus();

            if (status == SoundStatus.Playing)
            {
                Pause();
            }
            else if (_isPaused)
            {
                Resume();
            }
        }

        public void Stop()
        {
            // 2. Tell C to stop playing immediately
            try 
            {
                StopAudioNative();
            }
            catch (DllNotFoundException) { /* Safety if DLL missing during design time */ }

            // 3. Clean up memory (Existing code)
            if (_pinnedHandle.IsAllocated)
                _pinnedHandle.Free();
            
            _currentAudioData = null;
            _isPlaying = false;
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
