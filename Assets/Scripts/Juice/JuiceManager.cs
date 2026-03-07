using System.Collections;
using UnityEngine;

/// <summary>
/// Central juice manager — singleton providing static helpers for all game-feel effects.
/// Attach to any persistent GameObject in the scene (e.g. GameManager).
/// </summary>
public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ─────────────────────────────────────────────────────────
    // Coroutine helpers (called by Cell / Board / Block directly)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Punch-scale a transform: squash quickly then spring back to original scale.
    /// </summary>
    public Coroutine PunchScale(Transform target, float punchAmount = 0.35f, float duration = 0.25f)
        => StartCoroutine(PunchScaleRoutine(target, punchAmount, duration));

    /// <summary>
    /// Pop-in animate: scale from 0 → overshoot → settle at targetScale.
    /// Pass the desired final scale explicitly to avoid reading zero at call time.
    /// </summary>
    public Coroutine PopIn(Transform target, Vector3 targetScale, float duration = 0.22f)
        => StartCoroutine(PopInRoutine(target, targetScale, duration));

    /// <summary>
    /// Fade + scale-out (for cleared cells).
    /// </summary>
    public Coroutine ClearCell(SpriteRenderer sr, Transform t, float delay = 0f, float duration = 0.22f)
        => StartCoroutine(ClearCellRoutine(sr, t, delay, duration));

    /// <summary>
    /// Flash white, then fade → clear (used for line-clear cells).
    /// </summary>
    public Coroutine FlashAndClear(SpriteRenderer sr, Transform t, float delay = 0f)
        => StartCoroutine(FlashAndClearRoutine(sr, t, delay));

    // ─────────────────────────────────────────────────────────
    // Routines
    // ─────────────────────────────────────────────────────────

    private IEnumerator PunchScaleRoutine(Transform target, float punchAmount, float duration)
    {
        if (target == null) yield break;
        Vector3 originalScale = target.localScale;
        float halfDuration = duration * 0.5f;

        // Phase 1: squash to small 
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (target == null) yield break;
            float t = elapsed / halfDuration;
            float scale = 1f + punchAmount * Mathf.Sin(t * Mathf.PI);
            target.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: spring back with slight overshoot using elastic ease
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (target == null) yield break;
            float t = elapsed / halfDuration;
            // Elastic-out feeling
            float scale = 1f + (punchAmount * 0.3f) * Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
            target.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
            target.localScale = originalScale;
    }

    private IEnumerator PopInRoutine(Transform target, Vector3 targetScale, float duration)
    {
        if (target == null) yield break;
        // Start from zero — caller must NOT pre-set scale to 0
        target.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            float t = elapsed / duration;
            // Elastic-overshoot: shoots past 1.0 then settles
            float scale = EaseOutBack(t);
            target.localScale = targetScale * Mathf.Max(0f, scale);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
            target.localScale = targetScale;
    }

    private IEnumerator ClearCellRoutine(SpriteRenderer sr, Transform t, float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (sr == null || t == null) yield break;

        Vector3 originalScale = t.localScale;
        Color originalColor = sr.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (sr == null || t == null) yield break;
            float prog = elapsed / duration;
            float eased = prog * prog; // ease in (accelerate)
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - eased);
            t.localScale = originalScale * Mathf.Lerp(1f, 0f, eased);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (t != null) t.localScale = originalScale;
        if (sr != null) sr.color = originalColor;
    }

    private IEnumerator FlashAndClearRoutine(SpriteRenderer sr, Transform t, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        // Guard: object may have been hidden before delay elapsed
        if (sr == null || t == null || !t.gameObject.activeSelf) yield break;

        Color originalColor = sr.color;
        Vector3 originalScale = t.localScale;

        // Quick flash to white
        float flashDuration = 0.06f;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            if (sr == null || t == null) yield break;
            float t2 = elapsed / flashDuration;
            sr.color = Color.Lerp(originalColor, Color.white, t2);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale pop up then shrink to 0
        float clearDuration = 0.18f;
        elapsed = 0f;
        while (elapsed < clearDuration)
        {
            if (sr == null || t == null) yield break;
            float prog = elapsed / clearDuration;
            float scale = prog < 0.3f
                ? Mathf.Lerp(1f, 1.25f, prog / 0.3f)
                : Mathf.Lerp(1.25f, 0f, (prog - 0.3f) / 0.7f);
            t.localScale = originalScale * scale;
            sr.color = new Color(1f, 1f, 1f, 1f - prog);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // === FIX: hide cell after animation completes ===
        // Restore scale/color first so the object is clean if re-activated later
        if (t != null) t.localScale = originalScale;
        if (sr != null) sr.color = originalColor;
        if (t != null) t.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // Easing functions
    // ─────────────────────────────────────────────────────────

    /// <summary>Goes slightly past 1.0, then settles — "back ease out".</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
