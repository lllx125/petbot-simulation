using UnityEngine;

public class MicrophoneCapture : MonoBehaviour
{
    [Header("Target format (match ESP)")]
    public int sampleRate = 16000;          // 16 kHz
    public int frameMs = 20;                // 20 ms frames -> 320 samples -> 640 bytes
    public int bufferSeconds = 2;           // ring buffer length

    [Header("Device (leave blank = first)")]
    public string deviceName = "";

    [Header("Logging Options")]
    public bool logVolume = false;

    private AudioClip micClip;
    private int channels = 1;
    private int clipSamples;                // samples per channel in the clip
    private int lastMicPos = 0;

    // scratch buffers
    private float[] floatFrame;             // interleaved (if stereo)
    private short[] pcm16Mono;              // 16-bit mono samples
    private byte[] pcm16LE;                 // little-endian bytes

    void Start()
    {
        var devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("No microphone devices found.");
            enabled = false;
            return;
        }
        if (string.IsNullOrEmpty(deviceName)) deviceName = devices[0];

        // lengthSec is our ring buffer size
        micClip = Microphone.Start(deviceName, true, bufferSeconds, sampleRate);

        // Wait until mic actually starts
        while (Microphone.GetPosition(deviceName) <= 0) { }

        channels = micClip.channels; // device-defined; we’ll downmix to mono
        clipSamples = micClip.samples; // per channel

        int frameSamples = sampleRate * frameMs / 1000;
        floatFrame = new float[frameSamples * channels];
        pcm16Mono = new short[frameSamples];
        pcm16LE = new byte[frameSamples * 2];

        Debug.Log($"Mic '{deviceName}' started: {sampleRate} Hz, {channels} ch, buffer {bufferSeconds}s");
    }

    void Update()
    {
        if (logVolume)
        {
            LogVolumeInfo();
        }
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);
    }

    /// Get latest N ms as PCM16 little-endian (default = frameMs).
    /// Returns a byte[] of length samples*2 (reused each call).
    public byte[] GetLatestPcm16LE(int ms = -1)
    {
        if (micClip == null) return null;
        if (ms <= 0) ms = frameMs;

        int needSamples = sampleRate * ms / 1000;
        EnsureBuffers(needSamples);

        int micPos = Microphone.GetPosition(deviceName); // current write head (per channel)
        if (micPos < 0 || micPos > clipSamples) return null;

        // Compute start index for the frame (per channel)
        int start = micPos - needSamples;
        if (start < 0) start += clipSamples;

        // Pull from AudioClip (handles wrap by two reads if needed)
        if (start + needSamples <= clipSamples)
        {
            micClip.GetData(floatFrame, start);
        }
        else
        {
            // wrap-around: split into tail + head
            int tail = clipSamples - start;
            var tmp = new float[needSamples * channels];
            micClip.GetData(tmp, start);
            micClip.GetData(tmp, 0); // overwrite beginning with head… so do it in two steps:
            // Copy tail then head into floatFrame
            // (tmp now contains 'head' only; re-read properly)
            micClip.GetData(floatFrame, start);             // tail into floatFrame[0..tail*ch)
            micClip.GetData(floatFrame, 0);                 // head into floatFrame[tail*ch..)
        }

        // Downmix to mono and convert to PCM16
        if (channels == 1)
        {
            for (int i = 0; i < needSamples; i++)
            {
                float f = Mathf.Clamp(floatFrame[i], -1f, 1f);
                pcm16Mono[i] = (short)Mathf.RoundToInt(f * 32767f);
            }
        }
        else
        {
            for (int i = 0, k = 0; i < needSamples; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++, k++) sum += floatFrame[k];
                float m = Mathf.Clamp(sum / channels, -1f, 1f);
                pcm16Mono[i] = (short)Mathf.RoundToInt(m * 32767f);
            }
        }

        // Pack little-endian (matches ESP int16_t memory layout)
        for (int i = 0, j = 0; i < needSamples; i++, j += 2)
        {
            short s = pcm16Mono[i];
            pcm16LE[j] = (byte)(s & 0xFF);   // low
            pcm16LE[j + 1] = (byte)((s >> 8) & 0xFF); // high
        }

        lastMicPos = micPos;
        return pcm16LE;
    }

    /// RMS level in the latest frame (0..1)
    public float GetRms(int ms = -1)
    {
        var bytes = GetLatestPcm16LE(ms);
        if (bytes == null) return 0f;

        int samples = bytes.Length / 2;
        double acc = 0;
        for (int i = 0; i < samples; i++)
        {
            short s = (short)(bytes[2 * i] | (bytes[2 * i + 1] << 8));
            double f = s / 32768.0;
            acc += f * f;
        }
        return (float)Mathf.Sqrt((float)(acc / samples));
    }

    /// Logs volume information to debug console
    public void LogVolumeInfo(int ms = -1)
    {
        float rms = GetRms(ms);
        float db = rms > 0 ? 20f * Mathf.Log10(rms) : -80f;
        Debug.Log($"Volume - RMS: {rms:F4}, dB: {db:F1}, Device: {deviceName}");
    }

    private void EnsureBuffers(int needSamples)
    {
        if (floatFrame == null || floatFrame.Length != needSamples * channels)
            floatFrame = new float[needSamples * channels];
        if (pcm16Mono == null || pcm16Mono.Length != needSamples)
            pcm16Mono = new short[needSamples];
        if (pcm16LE == null || pcm16LE.Length != needSamples * 2)
            pcm16LE = new byte[needSamples * 2];
    }
}
