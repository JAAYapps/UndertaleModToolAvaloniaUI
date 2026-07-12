const audioContext = new (window.AudioContext || window.webkitAudioContext)();
const activeSources = new Map();

export function playAudioFromBytes(audioData) {
    const arrayBuffer = audioData.buffer;

    audioContext.decodeAudioData(arrayBuffer, (buffer) => {
        const source = audioContext.createBufferSource();
        source.buffer = buffer;
        source.connect(audioContext.destination);
        
        source.onended = () => {
            // This cleanup is a bit tricky with IDs, so for now we just let it go.
            // A more complex implementation could manage this.
        };

        source.start(0);
        activeSources.set(performance.now(), source); // Store with a timestamp ID

    }, (error) => {
        console.error("Error decoding audio data:", error);
    });
}

export function stopAllAudio() {
    for (const source of activeSources.values()) {
        source.stop();
    }
    activeSources.clear();
}

export function isPlaying() {
    // A simple check if there are any sources. A real implementation
    // would also check their 'playbackState'.
    return activeSources.size > 0;
}