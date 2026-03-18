using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelPoolUI : MonoBehaviour
{
    [Header("Next Piece Preview (Dimmed)")]
    [SerializeField] private GameObject nextPiecePanel;
    [SerializeField] private TextMeshProUGUI nextTitleText;
    [SerializeField] private RawImage nextPieceImage;
    [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.5f;

    [Header("Pool Summary (Visual List)")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private Transform summaryContent;
    [SerializeField] private GameObject summaryRowTemplate;

    private List<GameObject> activeRows = new List<GameObject>();

    private void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
        if (summaryRowTemplate != null) summaryRowTemplate.SetActive(false);
        Refresh();
    }

    private void OnEnable()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.OnPoolChanged += Refresh; // Triggered after every DrawFromPool
            LevelModeManager.Instance.OnGoalUpdated += Refresh;
        }
    }

    private void OnDisable()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.OnPoolChanged -= Refresh;
            LevelModeManager.Instance.OnGoalUpdated -= Refresh;
        }
    }

    public void ToggleSummary()
    {
        if (summaryPanel == null) return;
        summaryPanel.SetActive(!summaryPanel.activeSelf);
        if (summaryPanel.activeSelf) Refresh();
    }

    public void Refresh()
    {
        var mgr = LevelModeManager.Instance;
        if (mgr == null || mgr.CurrentLevel == null || !mgr.CurrentLevel.HasSpawnPool)
        {
            if (nextPiecePanel != null) nextPiecePanel.SetActive(false);
            return;
        }

        if (nextPiecePanel != null) nextPiecePanel.SetActive(true);

        // 1. Update Next Piece Preview
        var (nextIdx, nextElems) = mgr.PeekNext();
        if (nextIdx >= 0)
        {
            UpdatePieceImage(nextPieceImage, nextIdx, nextElems, dimmedAlpha);
            if (nextTitleText != null) nextTitleText.text = "NEXT";
        }
        else
        {
            if (nextPieceImage != null) nextPieceImage.enabled = false;
            if (nextTitleText != null) nextTitleText.text = "POOL EMPTY";
        }

        // 2. Update Visual Summary
        if (summaryPanel != null && summaryPanel.activeSelf)
        {
            UpdateVisualSummary();
        }
    }

    private void UpdateVisualSummary()
    {
        if (summaryContent == null || summaryRowTemplate == null) return;

        foreach (var row in activeRows) Destroy(row);
        activeRows.Clear();

        // We need shapes with their designed elements. 
        // Note: The pool draw index tells us where to start counting from.
        var mgr = LevelModeManager.Instance;
        var summary = mgr.GetPoolSummary(); 

        var sortedKeys = new List<int>(summary.Keys);
        sortedKeys.Sort();

        foreach (int shapeIdx in sortedKeys)
        {
            GameObject row = Instantiate(summaryRowTemplate, summaryContent);
            row.SetActive(true);
            activeRows.Add(row);

            RawImage img = row.GetComponentInChildren<RawImage>();
            if (img != null)
            {
                // To show elements in summary, we'd need to know which elements this shape has in the pool.
                // For simplicity, we'll draw one instance of this shape from the remainders.
                UpdatePieceImage(img, shapeIdx, null, 1.0f); 
            }

            TextMeshProUGUI txt = row.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = $"x{summary[shapeIdx]}";
        }
    }

    private void UpdatePieceImage(RawImage rawImg, int shapeIdx, Element[] elements, float alpha)
    {
        if (rawImg == null) return;
        rawImg.enabled = true;

        int[,] poly = Polyominos.Get(shapeIdx);
        int rows = poly.GetLength(0);
        int cols = poly.GetLength(1);

        // Create a higher res texture for clear cells (e.g. 5x5 cells, each cell 8x8 pixels)
        int p = 12; // pixels per cell
        Texture2D tex = new Texture2D(5 * p, 5 * p);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color borderColor = new Color(0, 0, 0, 0.3f * alpha);

        // Initialize empty
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                tex.SetPixel(x, y, transparent);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (poly[r, c] == 0) continue;

                // Determine color
                Element elem = (elements != null && elements.Length == 25) 
                    ? elements[r * 5 + c] 
                    : Element.Normal;
                
                Color cellColor = GetElementColor(elem);
                cellColor.a *= alpha;

                // Draw cell with border
                for (int py = 0; py < p; py++)
                {
                    for (int px = 0; px < p; px++)
                    {
                        bool isBorder = (px == 0 || px == p - 1 || py == 0 || py == p - 1);
                        tex.SetPixel(c * p + px, r * p + py, isBorder ? borderColor : cellColor);
                    }
                }
            }
        }

        tex.Apply();
        rawImg.texture = tex;
    }

    private Color GetElementColor(Element e) => e switch
    {
        Element.Lightning => new Color(1f, 1f, 0.4f),
        Element.Fire      => new Color(1f, 0.4f, 0.3f),
        Element.Ice       => new Color(0.4f, 0.8f, 1f),
        _                 => new Color(0.25f, 0.65f, 1f) // Standard piece blue
    };
}
