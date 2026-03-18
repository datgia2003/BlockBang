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
    private Coroutine pickupJuice; // track so we can cancel before restoring scale

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

    public void Show(int polyominoIndex, Element[] forcedElements = null)
    {
        this.polyominoIndex = polyominoIndex;
        Hide();
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        center = new UnityEngine.Vector2(polyominoColumns * 0.5f, polyominoRows * 0.5f);

        // assign elements for each block cell
        for (var r = 0; r < polyominoRows; ++r)
        {
            for (var c = 0; c < polyominoColumns; ++c)
            {
                elementMap[r, c] = Element.Normal;
                if (polyomino[r, c] > 0)
                {
                    cells[r, c].transform.localPosition = new(c - center.x + 0.5f, r - center.y + 0.5f, 0.0f);
                    
                    // Choose element type:
                    Element elem = Element.Normal;
                    if (forcedElements != null && forcedElements.Length == 25)
                    {
                        elem = forcedElements[r * 5 + c];
                    }
                    
                    // Only randomize if NOT in level mode and no specific element was forced
                    if (elem == Element.Normal && (LevelModeManager.Instance == null || !LevelModeManager.Instance.IsLevelModeActive))
                    {
                        elem = elementRegistry.ChooseRandomElement();
                    }

                    elementMap[r, c] = elem;
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
        if (blocks.IsDiscardMode)
        {
            blocks.Discard(this);
            return;
        }

        inputPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.localPosition = position + inputOffset;
        transform.localScale = Vector3.one;

        blocks.ResetSortingOrder();
        SetSortingOrder(1);

        // === SOUND ===
        SoundManager.Instance?.Play(SoundManager.SFX.BlockPickup);

        // === JUICE: punch scale on pick-up ===
        CancelPickupJuice();
        if (JuiceManager.Instance != null)
            pickupJuice = JuiceManager.Instance.PunchScale(transform, 0.25f, 0.18f);

        currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
        board.Hover(currentDragPoint, polyominoIndex, elementMap);
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
                board.Hover(currentDragPoint, polyominoIndex, elementMap);
                Highlight(board.HighlightPolyominoColumns, board.HighlightPolyominoRows);
            }
        }
    }
    private void OnMouseUp()
    {
        previousMousePosition = Vector3.positiveInfinity;

        // Stop the pickup punch coroutine — don't let it override scale anymore
        CancelPickupJuice();

        currentDragPoint = Vector2Int.RoundToInt((Vector2)transform.position - center);
        bool placed = board.Place(currentDragPoint, polyominoIndex, elementMap);

        // === Restore position & scale FIRST, before any further juice ===
        transform.localPosition = position;
        transform.localScale = scale;

        if (placed)
        {
            // === SOUND ===
            SoundManager.Instance?.Play(SoundManager.SFX.BlockPlace);
            // === JUICE ===
            if (ScreenShake.Instance != null)
                ScreenShake.Instance.Shake(0.10f, 0.06f);
            gameObject.SetActive(false);
            blocks.Remove(this);
        }
        else
        {
            // === SOUND ===
            SoundManager.Instance?.Play(SoundManager.SFX.BlockInvalid);
            // === JUICE: invalid drop — wobble from correct base scale ===
            if (JuiceManager.Instance != null)
                JuiceManager.Instance.PunchScale(transform, 0.15f, 0.20f);
        }
    }

    private void CancelPickupJuice()
    {
        if (pickupJuice != null && JuiceManager.Instance != null)
        {
            JuiceManager.Instance.StopCoroutine(pickupJuice);
            pickupJuice = null;
        }
        // Do NOT restore localScale here — caller is responsible for setting the correct scale
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
