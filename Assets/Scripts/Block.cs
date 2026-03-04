using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Block : MonoBehaviour
{
    public const int Size = 5;
    private readonly Vector3 inputOffset = new(0.0f, 2.0f, 0.0f);
    [SerializeField] private Cell cellPrefabs;
    [SerializeField] private Blocks blocks;
    [SerializeField] private Board board;

    [SerializeField] private ElementRegistry elementRegistry;

    private int polyominoIndex;
    private SortingGroup sortingGroup;
    private readonly Cell[,] cells = new Cell[Size, Size];
    private Element[,] elementMap = new Element[Size, Size];
    private Vector3 position;
    private Vector3 scale;
    private Vector2 inputPoint;
    private Vector3 previousMousePosition = Vector3.positiveInfinity;
    private Vector2Int previousDragPoint;
    private Vector2Int currentDragPoint;
    private Vector2 center;
    private Camera mainCamera;

    void Awake()
    {
        sortingGroup = gameObject.GetComponent<SortingGroup>();
        mainCamera = Camera.main;
    }
    public void Initialize()
    {
        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                cells[r, c] = Instantiate(cellPrefabs, transform);
            }
        }
        position = transform.localPosition;
        scale = transform.localScale;
    }

    public void Show(int polyominoIndex)
    {
        this.polyominoIndex = polyominoIndex;
        Hide();
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        center = new UnityEngine.Vector2(polyominoColumns * 0.5f, polyominoRows * 0.5f);

        // assign random elements for each block cell
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                elementMap[r, c] = Element.Normal;
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].transform.localPosition = new(c - center.x + 0.5f, r - center.y + 0.5f, 0.0f);
                    
                    // Choose element type for this cell using the registry
                    var elem = elementRegistry.ChooseRandomElement();
                    elementMap[r, c] = elem;
                    // The visual update will be handled by Cell.cs, which now needs the registry
                    cells[r, c].SetElement(elem, elementRegistry.GetElementData(elem));
                    cells[r, c].Normal();
                }
                else
                {
                    cells[r, c].Hide();
                }
            }
        }
    }

    private void Hide()
    {
        for (var r = 0; r < Size; ++r)
        {
            for (var c = 0; c < Size; ++c)
            {
                cells[r, c].Hide();
            }
        }
    }

    private void OnMouseDown()
    {
        inputPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.localPosition = position + inputOffset;
        transform.localScale = Vector3.one;

        blocks.ResetSortingOrder();
        SetSortingOrder(1);


        currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
        board.Hover(currentDragPoint, polyominoIndex);
        Highlight(board.HighlightPolyominoColumns, board.HighlightPolyominoRows);

        previousDragPoint = currentDragPoint;
        previousMousePosition = Input.mousePosition;
    }
    private void OnMouseDrag()
    {
        var currentMousePosition = Input.mousePosition;
        if (currentMousePosition != previousMousePosition)
        {
            previousMousePosition = currentMousePosition;

            var inputDelta = (Vector2)mainCamera.ScreenToWorldPoint(Input.mousePosition) - inputPoint;
            transform.localPosition = position + inputOffset + (Vector3)inputDelta * 1.4f;

            currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
            if (currentDragPoint != previousDragPoint)
            {
                previousDragPoint = currentDragPoint;
                board.Hover(currentDragPoint, polyominoIndex);
                Highlight(board.HighlightPolyominoColumns, board.HighlightPolyominoRows);
            }
        }
    }
    private void OnMouseUp()
    {
        previousMousePosition = Vector3.positiveInfinity;
        currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
        if (board.Place(currentDragPoint, polyominoIndex, elementMap))
        {
            gameObject.SetActive(false);
            blocks.Remove();
        }
        transform.localPosition = position;
        transform.localScale = scale;

    }
    private void Highlight(List<int> fullLineColumns, List<int> fullLineRows)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        Unhighlight(polyominoColumns, polyominoRows, polyomino);

        HighlightColumns(polyominoRows, polyomino, fullLineColumns);
        HighlightRows(polyominoColumns, polyomino, fullLineRows);

    }
    private void Unhighlight(int polyominoColumns, int polyominoRows, int[,] polyomino)
    {
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].Normal();
                }
            }
        }
    }
    private void HighlightColumns(int polyominoRows, int[,] polyomino, List<int> fullLineColumns)
    {
        foreach (var c in fullLineColumns)
        {
            for (var r = 0; r < polyominoRows; ++r)
            {
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }
    private void HighlightRows(int polyominoColumns, int[,] polyomino, List<int> fullLineRows)
    {
        foreach (var r in fullLineRows)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].Highlight();
                }
            }
        }
    }
    public void SetSortingOrder(int order)
    {
        sortingGroup.sortingOrder = order;
    }
}
