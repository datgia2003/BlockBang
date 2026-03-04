using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private Sprite normal;
    [SerializeField] private Sprite highlight;
    private SpriteRenderer spriteRenderer;

    private ElementData currentElementData;
    public Element ElementType { get; private set; } = Element.Normal;

    void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void Normal()
    {
        gameObject.SetActive(true);
        spriteRenderer.sprite = normal;
        ApplyElementVisuals();
    }

    public void Highlight()
    {
        gameObject.SetActive(true);
        spriteRenderer.sprite = highlight;
        ApplyElementVisuals();
    }
    public void Hover()
    {
        gameObject.SetActive(true);
        spriteRenderer.sprite = normal;
        // semi-transparent hover
        spriteRenderer.color = new(1.0f, 1.0f, 1.0f, 0.5f);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Set element type for this cell and update its visual
    /// </summary>
    public void SetElement(Element element, ElementData elementData)
    {
        ElementType = element;
        currentElementData = elementData;
        ApplyElementVisuals();
    }

    private void ApplyElementVisuals()
    {
        // Use color from ElementData, or default to white if no special element.
        spriteRenderer.color = (currentElementData != null) ? currentElementData.ElementColor : Color.white;
    }
}
