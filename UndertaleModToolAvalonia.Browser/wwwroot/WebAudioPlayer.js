const audioContext = new (window.AudioContext || window.webkitAudioContext)();
const activeSources = new Map();
let nextSourceId = 0;

globalThis.WebAudioPlayer = {
    // Play audio data and return a unique ID for this sound instance.
    playAudioFromBytes: (audioData) => {
        const sourceId = nextSourceId++;
        const arrayBuffer = audioData.buffer;

        audioContext.decodeAudioData(arrayBuffer, (buffer) => {
            const source = audioContext.createBufferSource();
            source.buffer = buffer;
            source.connect(audioContext.destination);

            // When the sound finishes playing naturally, remove it from our active list.
            source.onended = () => {
                activeSources.delete(sourceId);
            };

            source.start(0); // Play the sound.

            // Store this sound source so we can control it later.
            activeSources.set(sourceId, source);

        }, (error) => {
            console.error(`Error decoding audio for sourceId ${sourceId}:`, error);
        });

        // We can return the ID here if C# needs to control individual sounds.
        // For now, C# will control all sounds at once.
    },

    // Stop a specific sound by its ID.
    stopAudio: (sourceId) => {
        if (activeSources.has(sourceId)) {
            activeSources.get(sourceId).stop(); // This will also trigger the 'onended' cleanup.
            activeSources.delete(sourceId);
        }
    },

    // Stop all currently playing sounds.
    stopAllAudio: () => {
        for (const source of activeSources.values()) {
            source.stop();
        }
        activeSources.clear();
    },

    // Check if any sound is currently playing.
    isPlaying: () => {
        return activeSources.size > 0;
    }
};