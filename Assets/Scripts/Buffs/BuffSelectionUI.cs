using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a TFT-style card selection panel when a buff milestone is reached.
/// • Pauses the game (Time.timeScale = 0).
/// • Displays 2–3 buff cards to choose from.
/// • On choice, resumes the game and notifies BuffManager.
/// </summary>
public class BuffSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Card Slots (assign 3 card root GameObjects)")]
    [SerializeField] private BuffCardUI[] cardSlots;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration  = 0.3f;
    [SerializeField] private float cardStaggerDelay = 0.08f;

    private Coroutine fadeCoroutine;

    // ── Unity Lifecycle ────────────────────────────────────────

    private void Awake()
    {
        // Ẩn panel ngay từ đầu, bất kể trạng thái trong Editor
        if (panel != null)
            panel.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────

    public void Show(List<BuffDefinition> offered)
    {
        panel.SetActive(true);
        Time.timeScale = 0f;

        // Bind cards
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i < offered.Count)
            {
                cardSlots[i].gameObject.SetActive(true);
                cardSlots[i].Bind(offered[i], this, i);
            }
            else
            {
                cardSlots[i].gameObject.SetActive(false);
            }
        }

        // Fade in
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    /// <summary>Called by BuffCardUI when a card is clicked.</summary>
    public void OnCardChosen(BuffDefinition chosen)
    {
        BuffManager.Instance?.ApplyBuff(chosen);
        Hide();
    }

    // ── Fade animation (unscaled time so it works at timeScale=0) ──

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed  = 0f;

        // Stagger cards
        for (int i = 0; i < cardSlots.Length; i++)
            if (cardSlots[i].gameObject.activeSelf)
                cardSlots[i].PlayEnterAnimation(i * cardStaggerDelay);

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
