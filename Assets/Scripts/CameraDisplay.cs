// CameraDisplay.cs
using UnityEngine;
using UnityEngine.UI;

public class CameraDisplay : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    [SerializeField] private CameraCapture cameraCapture;

    Texture2D lastTex;

    void Start()
    {
        if (rawImage == null) rawImage = GetComponent<RawImage>();
        if (cameraCapture == null) cameraCapture = FindObjectOfType<CameraCapture>();

        if (rawImage == null)
        {
            Debug.LogError("RawImage component not found!");
            enabled = false;
            return;
        }
        if (cameraCapture == null)
        {
            Debug.LogError("CameraCapture component not found in scene!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (cameraCapture == null || rawImage == null) return;
        if (!cameraCapture.IsReady) return;

        var tex = cameraCapture.GetDisplayTexture();
        if (tex == null) return;

        if (tex != lastTex)
        {
            rawImage.texture = tex;
            rawImage.SetNativeSize();  // ensures the UI element matches 320x240 px
            lastTex = tex;
        }
        // No need to reassign every frame; texture data is updated in-place
    }
}
