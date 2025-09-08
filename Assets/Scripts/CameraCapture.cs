using UnityEngine;

/// Captures webcam, forces exact size (default 320x240), outputs RGB565 BIG-ENDIAN (ESP-like).
/// For on-screen display, it byte-swaps to little-endian before Upload.
public class CameraCapture : MonoBehaviour
{
    [Header("Match ESP32/OV2640")]
    public int targetWidth = 320;
    public int targetHeight = 240;
    public int targetFPS = 30;

    WebCamTexture cam;
    Color32[] src;            // camera pixels
    Color32[] resampled;      // exact target size
    byte[] rgb565BE;          // ESP-order (MSB first) bytes
    byte[] rgb565LE;          // temp buffer for Unity display (LSB first)
    Texture2D displayTex;     // TextureFormat.RGB565

    void Start()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length == 0) { Debug.LogError("No camera devices found"); return; }
        cam = new WebCamTexture(devices[0].name, targetWidth, targetHeight, targetFPS);
        cam.Play();
    }

    void OnDestroy()
    {
        if (cam != null) { cam.Stop(); cam = null; }
    }

    public bool IsReady => cam != null && cam.isPlaying && cam.width > 16 && cam.height > 16;

    /// Returns RGB565 bytes in BIG-ENDIAN (high byte, then low) to match ESP.
    public byte[] GetRGB565()
    {
        if (!IsReady) return null;

        int sw = cam.width, sh = cam.height;
        int srcLen = sw * sh;
        if (src == null || src.Length != srcLen) src = new Color32[srcLen];
        cam.GetPixels32(src);

        int dstLen = targetWidth * targetHeight;
        if (resampled == null || resampled.Length != dstLen) resampled = new Color32[dstLen];
        if (rgb565BE == null || rgb565BE.Length != dstLen * 2) rgb565BE = new byte[dstLen * 2];

        if (sw != targetWidth || sh != targetHeight)
            ResizeNearest(src, sw, sh, resampled, targetWidth, targetHeight);
        else
            System.Array.Copy(src, resampled, dstLen);

        ToRGB565BE(resampled, rgb565BE); // keep ESP order
        return rgb565BE;
    }

    /// Returns a Texture2D for UI. Internally swaps BIG->LITTLE endian for Unity upload.
    public Texture2D GetDisplayTexture()
    {
        var be = GetRGB565();
        if (be == null) return null;

        if (rgb565LE == null || rgb565LE.Length != be.Length)
            rgb565LE = new byte[be.Length];

        // Byte-swap for Unity (expects little-endian for TextureFormat.RGB565)
        for (int i = 0; i < be.Length; i += 2)
        {
            rgb565LE[i] = be[i + 1]; // low
            rgb565LE[i + 1] = be[i];     // high
        }

        if (displayTex == null || displayTex.width != targetWidth || displayTex.height != targetHeight)
            displayTex = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB565, false);

        displayTex.LoadRawTextureData(rgb565LE);
        displayTex.Apply(false, false);
        return displayTex;
    }

    static void ResizeNearest(Color32[] src, int sw, int sh, Color32[] dst, int dw, int dh)
    {
        for (int y = 0; y < dh; y++)
        {
            int sy = y * sh / dh;
            int syOff = sy * sw;
            int dyOff = y * dw;
            for (int x = 0; x < dw; x++)
            {
                int sx = x * sw / dw;
                dst[dyOff + x] = src[syOff + sx];
            }
        }
    }

    // Write BIG-ENDIAN RGB565 (MSB first) to match ESP raw buffers.
    static void ToRGB565BE(Color32[] src, byte[] dst)
    {
        int n = src.Length;
        for (int i = 0, j = 0; i < n; i++, j += 2)
        {
            var c = src[i];
            int r5 = c.r >> 3, g6 = c.g >> 2, b5 = c.b >> 3;
            ushort v = (ushort)((r5 << 11) | (g6 << 5) | b5);
            dst[j] = (byte)(v >> 8);   // high byte first
            dst[j + 1] = (byte)(v & 0xFF); // low byte
        }
    }
}
