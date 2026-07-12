#define MINIAUDIO_IMPLEMENTATION
#include "miniaudio.h"
#include <stdio.h>

#ifdef _WIN32
#define AUDIO_API __declspec(dllexport)
#else
#define AUDIO_API
#endif

// Global engine instance (keep it simple for now)
ma_engine engine;
ma_sound sound;     // We will reuse this sound container
ma_audio_buffer audioBuffer; // This wraps the raw data

// Initialize the engine once at startup
AUDIO_API int InitAudio() {
    ma_result result = ma_engine_init(NULL, &engine);
    if (result != MA_SUCCESS) return -1;
    return 0;
}

// Function to play raw float data passed from C#
// data: pointer to the float array from C#
// frameCount: total number of audio frames (samples / channels)
// channels: usually 2 for stereo
// sampleRate: usually 44100
AUDIO_API int PlayRawAudio(float* data, ma_uint64 frameCount, ma_uint32 channels, ma_uint32 sampleRate) {
    ma_result result;

    // 1. Uninitialize previous sound if it exists
    ma_sound_uninit(&sound);
    ma_audio_buffer_uninit(&audioBuffer);

    // 2. Configure the buffer to wrap YOUR data (No copying! Fast!)
    ma_audio_buffer_config bufferConfig = ma_audio_buffer_config_init(
        ma_format_f32,  // We are sending floats
        channels,
        frameCount,
        data,
        NULL // We manage memory allocation in C#, so no free callback needed
    );

    result = ma_audio_buffer_init(&bufferConfig, &audioBuffer);
    if (result != MA_SUCCESS) return result;

    // 3. Create a sound from this buffer
    result = ma_sound_init_from_data_source(&engine, &audioBuffer, 0, NULL, &sound);
    if (result != MA_SUCCESS) return result;

    // 4. Play it!
    ma_sound_start(&sound);

    return 0;
}

AUDIO_API void PauseAudio() {
    if (ma_sound_is_playing(&sound)) {
        ma_sound_stop(&sound);
    }
}

AUDIO_API void ResumeAudio() {
    // ma_sound_start automatically resumes from the last position
    if (!ma_sound_is_playing(&sound)) {
        ma_sound_start(&sound);
    }
}

// Make sure StopAudio rewinds, otherwise "Stop" will act like "Pause"
AUDIO_API void StopAudio() {
    if (ma_sound_is_playing(&sound)) {
        ma_sound_stop(&sound);
    }
    ma_sound_seek_to_pcm_frame(&sound, 0);
}

AUDIO_API void SetMasterVolume(float volume) {
    ma_engine_set_volume(&engine, volume);
}

AUDIO_API void SetPitch(float pitch) {
    if (ma_sound_is_playing(&sound)) {
        ma_sound_set_pitch(&sound, pitch);
    }
}
