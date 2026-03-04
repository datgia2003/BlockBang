using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Board : MonoBehaviour
{
    public const int Size = 8;
    [SerializeField] private Cell cellPrefabs;
    [SerializeField] private Transform cellsTransform;
    [SerializeField] private ElementRegistry elementRegistry; // New field for the registry

    private readonly Cell[,] cells = new Cell[Size, Size];
    private readonly int[,] data = new int[Size, Size];
    private readonly Element[,] elements = new Element[Size, Size];

    private readonly List<Vector2Int> hoverPoints = new();
    private readonly List<int> fullLineColumns = new();
    private readonly List<int> fullLineRows = new();
    private readonly List<int> highlightPolyominoColumns = new();
    private readonly List<int> highlightPolyominoRows = new();

    private bool isClearingLine = false;

    // count lightning effects triggered by the current clear cycle
    private int lightningCount = 0;

    void Start()
    {
        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                cells[r, c] = Instantiate(cellPrefabs, cellsTransform);
                cells[r, c].transform.position = new(c + 0.5f, r + 0.5f, 0.0f);
                cells[r, c].Hide();
            }
        }
    }

    #region Public API for Effects
    /// <summary>
    /// Checks if a given coordinate is within the board's bounds.
    /// </summary>
    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    /// <summary>
    /// Sets a cell's data back to an occupied state. Used by IceEffect.
    /// </summary>
    public void SetCellAsOccupied(int x, int y)
    {
        if (IsWithinBounds(x, y))
        {
            data[y, x] = 2;
        }
    }

    /// <summary>
    /// Changes the element type of a cell at a given position. Used by IceEffect.
    /// </summary>
    public void SetElementAt(int x, int y, Element newElement)
    {
        if (IsWithinBounds(x, y))
        {
            elements[y, x] = newElement;
            var elementData = elementRegistry.GetElementData(newElement);
            cells[y, x].SetElement(newElement, elementData);
            cells[y,x].Normal(); // Redraw the cell with the new element visuals
        }
    }

    /// <summary>
    /// Adds a specified number of charges to the lightning counter. Used by LightningEffect.
    /// </summary>
    public void AddLightningCharges(int amount)
    {
        lightningCount += amount;
    }
    #endregion

    public void Hover(Vector2Int point, int polyominoIndex)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        UnHover();
        Unhighlight();

        highlightPolyominoColumns.Clear();
        highlightPolyominoRows.Clear();

        HoverPoints(point, polyominoRows, polyominoColumns, polyomino);
        if (hoverPoints.Count > 0)
        {
            Hover();
            Highlight(point, polyominoColumns, polyominoRows);
            foreach (var c in fullLineColumns)
            {
                highlightPolyominoColumns.Add(c - point.x);
            }
            foreach (var r in fullLineRows)
            {
                highlightPolyominoRows.Add(r - point.y);
            }
        }

    }
    private void HoverPoints(Vector2Int point, int polyominoRows, int polyominoColumns, int[,] polyomino)
    {
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    var hoverPoint = point + new Vector2Int(c, r);
                    if (IsValidPoint(hoverPoint) == false)
                    {
                        hoverPoints.Clear();
                        return;
                    }
                    hoverPoints.Add(hoverPoint);
                }
            }
        }
    }
    private bool IsValidPoint(UnityEngine.Vector2Int point)
    {
        if (point.x < 0 || Size <= point.x) return false;
        if (point.y < 0 || Size <= point.y) return false;
        if (data[point.y, point.x] > 0) return false;

        return true;
    }
    private void Hover()
    {
        foreach (var hoverPoint in hoverPoints)
        {
            data[hoverPoint.y, hoverPoint.x] = 1;
            cells[hoverPoint.y, hoverPoint.x].Hover();
        }
    }
    private void UnHover()
    {
        foreach (var hoverPoint in hoverPoints)
        {
            data[hoverPoint.y, hoverPoint.x] = 0;
            cells[hoverPoint.y, hoverPoint.x].Hide();
        }
        hoverPoints.Clear();
    }
    
    public bool Place(Vector2Int point, int polyominoIndex, Element[,] blockElements)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        UnHover();
        HoverPoints(point, polyominoRows, polyominoColumns, polyomino);
        if (hoverPoints.Count > 0)
        {
            Place(point, polyominoColumns, polyominoRows, polyomino, blockElements);
            return true;
        }
        return false;
    }
    private void Place(Vector2Int point, int polyominoColumns, int polyominoRows, int[,] polyomino, Element[,] blockElements)
    {
        // place each cell and record element type
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    var boardPos = point + new Vector2Int(c, r);
                    data[boardPos.y, boardPos.x] = 2;
                    var elem = blockElements[r, c];
                    elements[boardPos.y, boardPos.x] = elem;
                    
                    var elementData = elementRegistry.GetElementData(elem);
                    cells[boardPos.y, boardPos.x].SetElement(elem, elementData);
                    cells[boardPos.y, boardPos.x].Normal();
                }
            }
        }
        ClearFullLines(point, polyominoColumns, polyominoRows);
        hoverPoints.Clear();
        SkillManager.Instance.OnBlockPlaced();
    }

    private void ClearFullLines(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        FullLineColumns(point.x, point.x + polyominoColumns);
        FullLineRows(point.y, point.y + polyominoRows);
        ClearFullLinesColumns();
        ClearFullLinesRows();
        AfterClearingEffects();
    }
    private void FullLineColumns(int from, int to)
    {
        fullLineColumns.Clear();
        // clamp to board dimensions
        from = Mathf.Clamp(from, 0, Size);
        to = Mathf.Clamp(to, 0, Size);
        if (from >= to) return;

        for (var c = from; c < to; ++c)
        {
            var isFullLine = true;
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine)
            {
                fullLineColumns.Add(c);
            }
        }
    }
    private void FullLineRows(int from, int to)
    {
        fullLineRows.Clear();
        // clamp to board dimensions
        from = Mathf.Clamp(from, 0, Size);
        to = Mathf.Clamp(to, 0, Size);
        if (from >= to) return;

        for (var r = from; r < to; ++r)
        {
            var isFullLine = true;
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine)
            {
                fullLineRows.Add(r);
            }
        }
    }

    private void ClearFullLinesColumns()
    {
        isClearingLine = true;
        foreach (var c in fullLineColumns)
        {
            ScoreManager.Instance.AddScore(40);
            for (var r = 0; r < Size; ++r)
            {
                HandleCellClear(r, c);
            }
        }
        isClearingLine = false;
    }
    private void ClearFullLinesRows()
    {
        isClearingLine = true;
        foreach (var r in fullLineRows)
        {
            ScoreManager.Instance.AddScore(40);
            for (var c = 0; c < Size; ++c)
            {
                HandleCellClear(r, c);
            }
        }
        isClearingLine = false;
    }

    public void HandleCellClear(int r, int c)
    {
        if (data[r, c] != 2) return; // Already cleared or empty
        if(!isClearingLine) ScoreManager.Instance.AddScore(5);
        var elemType = elements[r, c];
        var elementData = elementRegistry.GetElementData(elemType);

        // IMPORTANT: Clear the cell's data BEFORE executing the effect
        // to prevent infinite loops (e.g., Fire block triggering itself).
        data[r, c] = 0;
        elements[r, c] = Element.Normal;
        
        // Execute the element's effect, if it has one.
        if (elementData != null && elementData.Effect != null)
        {
            elementData.Effect.ExecuteEffect(this, new Vector2Int(c, r));
        }

        // If the cell is still unoccupied after the effect (e.g. not an Ice block), hide it.
        if (data[r,c] == 0)
        {
            cells[r, c].Hide();
        }
    }

    private void AfterClearingEffects()
    {
        // Use a while loop to handle cascading lightning effects.
        // This ensures that if a randomly cleared cell is ALSO a lightning block,
        // its charge is added and then consumed in the same clearing cycle.
        while (lightningCount > 0)
        {
            lightningCount--; // Consume one charge before clearing
            ClearRandomCell();
        }
    }

    private void ClearRandomCell()
    {
        var candidates = new List<Vector2Int>();
        for (int rr = 0; rr < Size; rr++)
        {
            for (int cc = 0; cc < Size; cc++)
            {
                if (data[rr, cc] == 2)
                {
                    candidates.Add(new Vector2Int(cc, rr));
                }
            }
        }
        if (candidates.Count == 0) return;
        var choice = candidates[Random.Range(0, candidates.Count)];
        HandleCellClear(choice.y, choice.x);
    }
    private void Highlight(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        PredictFullLineColumns(point.x, point.x + polyominoColumns);
        PredictFullLineRows(point.y, point.y + polyominoRows);
        HighlightFullLinesColumns();
        HighlightFullLinesRows();
    }
    private void PredictFullLineColumns(int from, int to)
    {
        fullLineColumns.Clear();
        // clamp to board boundaries
        from = Mathf.Clamp(from, 0, Size);
        to = Mathf.Clamp(to, 0, Size);
        if (from >= to) return;

        for (var c = from; c < to; ++c)
        {
            var isFullLine = true;
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] != 1 && data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine)
            {
                fullLineColumns.Add(c);
            }
        }
    }
    private void PredictFullLineRows(int from, int to)
    {
        fullLineRows.Clear();
        // clamp boundaries
        from = Mathf.Clamp(from, 0, Size);
        to = Mathf.Clamp(to, 0, Size);
        if (from >= to) return;

        for (var r = from; r < to; ++r)
        {
            var isFullLine = true;
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] != 1 && data[r, c] != 2)
                {
                    isFullLine = false;
                    break;
                }
            }
            if (isFullLine)
            {
                fullLineRows.Add(r);
            }
        }
    }
    private void HighlightFullLinesColumns()
    {
        foreach (var c in fullLineColumns)
        {
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }
    private void HighlightFullLinesRows()
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }
    private void UnhighlightFullLinesColumns()
    {
        foreach (var c in fullLineColumns)
        {
            for (var r = 0; r < Size; ++r)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Normal();
                }
            }
        }
    }
    private void UnhighlightFullLinesRows()
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < Size; ++c)
            {
                if (data[r, c] == 2)
                {
                    cells[r, c].Normal();
                }
            }
        }
    }
    private void Unhighlight()
    {
        UnhighlightFullLinesColumns();
        UnhighlightFullLinesRows();
    }
    public List<int> HighlightPolyominoColumns => highlightPolyominoColumns;
    public List<int> HighlightPolyominoRows => highlightPolyominoRows;
    public bool CheckPlace(int polyominoIndex)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        for (var r = 0; r <= Size - polyominoRows; ++r)
        {
            for (var c = 0; c <= Size - polyominoColumns; ++c)
            {
                if (CheckPlace(c, r, polyominoColumns, polyominoRows, polyomino))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool CheckPlace(int x, int y, int polyominoColumns, int polyominoRows, int[,] polyomino)
    {
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0 && data[y + r, x + c] == 2)
                {
                    return false;
                }
            }
        }
        return true;
    }
}