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
    private List<float> beatTimes = new List<float>(); // To track beat timestamps
    private int maxBeatHistory = 5; // Number of beats to average for BPM

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
            // Record beat time
            beatTimes.Add(Time.time);

            // Keep only recent beats
            if (beatTimes.Count > maxBeatHistory)
                beatTimes.RemoveAt(0);

            // Calculate and update BPM
            UpdateBPM();

            StartCoroutine(PulseHeart());
            lastBeatTime = Time.time;
        }

        heartObject.localScale = Vector3.Lerp(heartObject.localScale, originalScale, Time.deltaTime * scaleSpeed);
    }

    void UpdateBPM()
    {
        if (beatTimes.Count < 2)
        {
            // Not enough beats recorded yet
            if (bpmText != null)
                bpmText.text = "BPM: --";
            return;
        }

        // Calculate average time between beats
        float totalTime = 0;
        for (int i = 1; i < beatTimes.Count; i++)
        {
            totalTime += beatTimes[i] - beatTimes[i - 1];
        }
        float averageTimeBetweenBeats = totalTime / (beatTimes.Count - 1);

        // Calculate BPM
        float bpm = 60f / averageTimeBetweenBeats;

        // Update display
        if (bpmText != null)
            bpmText.text = "BPM: " + Mathf.RoundToInt(bpm);
    }

    IEnumerator PulseHeart()
    {
        Vector3 targetScale = originalScale * scaleMultiplier;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            heartObject.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            heartObject.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
    }
}

// void HandleAudioFrame(byte[] audioData, int sampleRate, int channels)
// {
//     float[] samples = ConvertToFloatArray(audioData);
//     AudioClip clip = AudioClip.Create("HeartbeatLive", samples.Length, channels, sampleRate, false);
//     clip.SetData(samples, 0);
//     PlayLiveAudio(clip);
// }