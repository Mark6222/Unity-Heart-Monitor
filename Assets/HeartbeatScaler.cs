using UnityEngine;

public class HeartbeatScaler : MonoBehaviour
{
    public Transform heartObject;
    public float scaleMultiplier = 1.2f;
    public float scaleSpeed = 5f;

    private Vector3 originalScale;

    void Start()
    {
        if (heartObject == null)
            heartObject = this.transform;

        originalScale = heartObject.localScale;
    }

    // Call this function with the heartbeat "intensity" (0.0 to 1.0)
    public void UpdateHeartScale(float intensity)
    {
        float targetScale = Mathf.Lerp(1f, scaleMultiplier, intensity);
        Vector3 newScale = originalScale * targetScale;

        heartObject.localScale = Vector3.Lerp(heartObject.localScale, newScale, Time.deltaTime * scaleSpeed);
    }
}
