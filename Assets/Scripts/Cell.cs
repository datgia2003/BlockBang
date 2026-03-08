using System.Collections;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [Tooltip("Sprite mặc định dùng khi không có ElementData hoặc ElementData không có sprite riêng.")]
    [SerializeField] private Sprite defaultNormal;
    [SerializeField] private Sprite defaultHighlight;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private Coroutine juiceCoroutine;

    private ElementData currentElementData;
    public Element ElementType { get; private set; } = Element.Normal;

    void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    // ─────────────────────────────────────────────────────────
    // State setters
    // ─────────────────────────────────────────────────────────

    public void Normal()
    {
        CancelJuice();           // stop any running animation first
        gameObject.SetActive(true);
        SetSprite(false);
        ApplyElementColor();
        transform.localScale = baseScale; // ensure clean state
    }

    public void Highlight()
    {
        CancelJuice();
        gameObject.SetActive(true);
        SetSprite(true);
        ApplyElementColor();
    }

    public void Hover(ElementData hoverElementData = null)
    {
        CancelJuice();
        gameObject.SetActive(true);
        // Show the ghost using the DRAGGED block's element, not whatever was here before
        var displayData = hoverElementData;
        Sprite hoverSprite = (displayData?.NormalSprite) ?? defaultNormal;
        spriteRenderer.sprite = hoverSprite;
        var baseColor = (displayData != null) ? displayData.ElementColor : Color.white;
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
    }

    /// <summary>Called by UnHover: hides and clears any stale element state so the
    /// next hover on this cell doesn't bleed the old element's visuals.</summary>
    public void HoverReset()
    {
        CancelJuice();
        gameObject.SetActive(false);
        // Clear element so stale sprite/color can't leak into the next Hover call
        ElementType = Element.Normal;
        currentElementData = null;
    }

    public void Hide()
    {
        CancelJuice();
        gameObject.SetActive(false);
    }

    public void SetElement(Element element, ElementData elementData)
    {
        ElementType = element;
        currentElementData = elementData;
    }

    // ─────────────────────────────────────────────────────────
    // Juice animations — called from Board
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pop-in animation when a block is placed onto the board.
    /// </summary>
    public void PlayPlaceAnimation()
    {
        if (JuiceManager.Instance == null) return;
        CancelJuice();
        gameObject.SetActive(true);
        // Pass baseScale explicitly; JuiceManager will set scale to 0 internally
        juiceCoroutine = JuiceManager.Instance.PopIn(transform, baseScale);
    }

    /// <summary>
    /// Flash white then scale-out. Called when this cell is part of a cleared line.
    /// delay staggers the animation across a wave.
    /// </summary>
    public void PlayClearAnimation(float delay = 0f)
    {
        if (JuiceManager.Instance == null) { Hide(); return; }
        CancelJuice();
        juiceCoroutine = JuiceManager.Instance.FlashAndClear(spriteRenderer, transform, delay);
        // Disable the cell after the animation completes from caller (Board does it)
    }

    /// <summary>
    /// Visual-only punch scale (e.g., when already-placed cell is part of a to-be-cleared line).
    /// </summary>
    public void PlayPunchScale()
    {
        if (JuiceManager.Instance == null) return;
        CancelJuice();
        juiceCoroutine = JuiceManager.Instance.PunchScale(transform);
    }

    // ─────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────

    private void SetSprite(bool isHighlight)
    {
        Sprite target;
        if (isHighlight)
            target = (currentElementData?.HighlightSprite) ?? defaultHighlight;
        else
            target = (currentElementData?.NormalSprite) ?? defaultNormal;
        spriteRenderer.sprite = target;
    }

    private void ApplyElementColor()
    {
        spriteRenderer.color = (currentElementData != null) ? currentElementData.ElementColor : Color.white;
    }

    private void CancelJuice()
    {
        if (juiceCoroutine != null)
        {
            JuiceManager.Instance?.StopCoroutine(juiceCoroutine);
            juiceCoroutine = null;
        }
        // Restore safe state
        transform.localScale = baseScale;
        if (spriteRenderer != null && currentElementData != null)
            spriteRenderer.color = currentElementData.ElementColor;
    }
}
