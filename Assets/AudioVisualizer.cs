using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class AudioVisualizer : MonoBehaviour
{
    public int points = 512;
    public float heightScale = 10f;
    private LineRenderer lineRenderer;
    private float[] samples;

    void Start()
    {
        samples = new float[points];
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points;
    }

    void Update()
    {
        AudioListener.GetOutputData(samples, 0);

        for (int i = 0; i < points; i++)
        {
            Vector3 pos = new Vector3(i * 0.01f, samples[i] * heightScale, 0);
            lineRenderer.SetPosition(i, pos);
        }
    }
}
