using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to Main Camera. Exposes Shake(duration, magnitude) để tạo screen shake.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        originalLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// Trigger screen shake.
    /// </summary>
    /// <param name="duration">Seconds the shake lasts.</param>
    /// <param name="magnitude">Pixel-space magnitude of the shake.</param>
    public void Shake(float duration = 0.18f, float magnitude = 0.12f)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            // Ease out: shake dampens over time
            float dampedMagnitude = Mathf.Lerp(magnitude, 0f, progress);
            float offsetX = Random.Range(-1f, 1f) * dampedMagnitude;
            float offsetY = Random.Range(-1f, 1f) * dampedMagnitude;
            transform.localPosition = originalLocalPosition + new Vector3(offsetX, offsetY, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }
}
