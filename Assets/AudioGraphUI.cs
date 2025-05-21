using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class AudioGraphUI : MonoBehaviour
{
    public RawImage graphImage;
    public int resolution = 512;
    private Texture2D texture;
    private float[] audioSamples;

    void Start()
    {
        audioSamples = new float[resolution];
        texture = new Texture2D(resolution, 100, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        graphImage.texture = texture;
    }

    void Update()
    {
        AudioListener.GetOutputData(audioSamples, 0);
        texture.SetPixels32(DrawWaveform(audioSamples));
        texture.Apply();
    }

    Color32[] DrawWaveform(float[] data)
    {
        Color32[] pixels = new Color32[resolution * 100];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 255);

        for (int x = 0; x < data.Length; x++)
        {
            int y = Mathf.Clamp((int)((data[x] + 1f) * 50f), 0, 99);
            pixels[x + y * resolution] = new Color32(0, 255, 0, 255); // Green line
        }

        return pixels;
    }
}
