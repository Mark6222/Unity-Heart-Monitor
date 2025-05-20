using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
public class HeartbeatAudioPulse : MonoBehaviour
{
    public Transform heartObject;             // Assign in Inspector
    public float scaleMultiplier = 1.2f;      // Max scale relative to base
    public float scaleSpeed = 5f;             // How fast the heart grows/shrinks
    public float beatThreshold = 0.1f;        // Volume threshold to trigger pulse
    public float cooldownTime = 0.3f;         // Minimum time between pulses

    private Vector3 originalScale;
    private float lastBeatTime = 0f;
    private float[] audioSamples = new float[128];

    public TextMeshProUGUI bpmText; // Assign in Inspector

    void Start()
    {
        if (heartObject == null)
            heartObject = transform;

        originalScale = heartObject.localScale;
    }

    void Update()
    {
        AudioListener.GetOutputData(audioSamples, 0);

        float volume = 0f;
        foreach (var sample in audioSamples)
            volume += Mathf.Abs(sample);
        volume /= audioSamples.Length;
        Debug.Log("Volume: " + volume);
        if (volume > beatThreshold && Time.time > lastBeatTime + cooldownTime)
        {
            StartCoroutine(PulseHeart());
            lastBeatTime = Time.time;
        }

        heartObject.localScale = Vector3.Lerp(heartObject.localScale, originalScale, Time.deltaTime * scaleSpeed);
    }

    IEnumerator PulseHeart()
    {
        Vector3 targetScale = originalScale * scaleMultiplier;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            heartObject.localScale = Vector3.Lerp(originalScale, targetScale, t);
            // Update the BPM text
            if (bpmText != null)
            {
                float bpm = 60f / cooldownTime;
                bpmText.text = "BPM: " + Mathf.RoundToInt(bpm);
            }
            yield return null;

        }

        t = 0f;
        while (t < 1f)
        {

            t += Time.deltaTime * scaleSpeed;
            heartObject.localScale = Vector3.Lerp(targetScale, originalScale, t);
            // Update the BPM text
            if (bpmText != null)
            {
                float bpm = 60f / cooldownTime;
                bpmText.text = "BPM: " + Mathf.RoundToInt(bpm);
            }
            yield return null;
        }
    }
}
