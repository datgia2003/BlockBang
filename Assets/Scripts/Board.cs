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
    // Buff 5: track occupied cells highlighted by 7-cell run prediction
    private readonly List<Vector2Int> sevenCellHighlightedCells = new();

    private bool isClearingLine = false;

    // ── Effect Chain (queued, coroutine-driven) ───────────────
    [Header("Effect Chain")]
    [Tooltip("Seconds between each Fire chain / Lightning strike visual step.")]
    [SerializeField] private float chainStepDelay = 0.20f;

    /// <summary>
    /// Queue of pending cell clears that should be processed one-by-one with a delay.
    /// Populated by HandleCellClear when called while a chain or line-clear is active.
    /// </summary>
    private struct PendingClear
    {
        public Vector2Int Position;
        public Element SourceElement;
        public GameObject VfxHandle;
    }
    private readonly Queue<PendingClear> pendingClears = new();

    /// <summary>True while ExecuteCellClear is running, so recursive Fire calls get deferred.</summary>
    private bool isRunningEffectChain = false;

    /// <summary>Counter for delayed operations that should paralyze the board (e.g. Fire's 2 second burn).</summary>
    private int activeDelayedEffects = 0;

    /// <summary>True while EffectChainRoutine coroutine is active — blocks new block placement.</summary>
    private bool isEffectChainActive = false;
    public bool IsEffectChainActive => isEffectChainActive || activeDelayedEffects > 0;

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

        // Check if we should start in level mode
        if (LevelModeManager.Instance != null && LevelModeManager.Instance.IsLevelModeActive)
        {
            LoadLevel(LevelModeManager.Instance.CurrentLevel);
        }
    }

    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null) return;

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                int index = r * Size + c;
                int cellData = levelData.InitialBoardData[index];
                Element element = levelData.InitialElements[index];

                data[r, c] = cellData;
                elements[r, c] = element;

                if (cellData == 2)
                {
                    var elementData = elementRegistry.GetElementData(element);
                    cells[r, c].SetElement(element, elementData);
                    cells[r, c].Normal();
                }
                else
                {
                    cells[r, c].Hide();
                }
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

    /// <summary>Plays a localized punch scale and particle burst when an ice block shatters to normal.</summary>
    public void PlayIceShatterAnim(Vector2Int position)
    {
        if (IsWithinBounds(position.x, position.y))
        {
            if (JuiceManager.Instance != null)
                JuiceManager.Instance.PunchScale(cells[position.y, position.x].transform, 0.65f, 0.35f);
            else
                cells[position.y, position.x].PlayPunchScale();

            var worldPos = cells[position.y, position.x].transform.position;
            ParticleBurst.Instance?.PopBurst(worldPos, new Color(0.6f, 0.95f, 1f));
        }
    }

    /// <summary>
    /// Enqueues a lightning target directly to the pending clears instead of just adding charges.
    /// </summary>
    public void EnqueueLightningClear(Vector2Int target, GameObject arcHandle)
    {
        pendingClears.Enqueue(new PendingClear
        {
            Position = target,
            SourceElement = Element.Lightning,
            VfxHandle = arcHandle
        });
        
        // Ensure chain routine runs if not active
        if (!isEffectChainActive && !isClearingLine)
            StartCoroutine(EffectChainRoutine());
    }

    /// <summary>
    /// Returns the world position of a board cell. Used by ElementVFX.
    /// </summary>
    public Vector3 GetCellWorldPosition(int x, int y)
    {
        if (IsWithinBounds(x, y))
            return cells[y, x].transform.position;
        return Vector3.zero;
    }

    /// <summary>
    /// Returns up to `count` random positions of currently occupied cells.
    /// Used by LightningEffect to preview VFX targets before the clear happens.
    /// </summary>
    public List<Vector2Int> PeekRandomOccupiedCells(int count)
    {
        var candidates = new List<Vector2Int>();
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (data[r, c] == 2)
                    candidates.Add(new Vector2Int(c, r));

        // Shuffle and take `count`
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
    }
    #endregion

    // Stores the element map of the block currently being hovered so Hover() can look up
    // the correct ElementData for each ghost cell.
    private Element[,] hoverElementMap;

    public void Hover(Vector2Int point, int polyominoIndex, Element[,] elementMap = null)
    {
        var polyomino = Polyominos.Get(polyominoIndex);
        var polyominoRows    = polyomino.GetLength(0);
        var polyominoColumns = polyomino.GetLength(1);
        UnHover();
        Unhighlight();

        highlightPolyominoColumns.Clear();
        highlightPolyominoRows.Clear();

        hoverElementMap = elementMap; // remember for ShowHover()

        HoverPoints(point, polyominoRows, polyominoColumns, polyomino);
        if (hoverPoints.Count > 0)
        {
            ShowHover(point, polyomino);
            Highlight(point, polyominoColumns, polyominoRows);
            foreach (var c in fullLineColumns)
                highlightPolyominoColumns.Add(c - point.x);
            foreach (var r in fullLineRows)
                highlightPolyominoRows.Add(r - point.y);
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
    /// <summary>Show ghost cells using the dragged block's element data.</summary>
    private void ShowHover(Vector2Int origin, int[,] polyomino)
    {
        int polyominoRows    = polyomino.GetLength(0);
        int polyominoColumns = polyomino.GetLength(1);
        foreach (var hp in hoverPoints)
        {
            // Map from board position back to polyomino local coords
            int pr = hp.y - origin.y;
            int pc = hp.x - origin.x;

            ElementData elemData = null;
            if (hoverElementMap != null
                && pr >= 0 && pr < polyominoRows
                && pc >= 0 && pc < polyominoColumns)
            {
                var elem = hoverElementMap[pr, pc];
                elemData = elementRegistry.GetElementData(elem);
            }

            data[hp.y, hp.x] = 1;
            cells[hp.y, hp.x].Hover(elemData);
        }
    }
    private void UnHover()
    {
        foreach (var hoverPoint in hoverPoints)
        {
            data[hoverPoint.y, hoverPoint.x] = 0;
            // HoverReset clears stale element data so old sprites don't bleed into next hover
            cells[hoverPoint.y, hoverPoint.x].HoverReset();
        }
        hoverPoints.Clear();
        hoverElementMap = null;
    }
    
    public bool Place(Vector2Int point, int polyominoIndex, Element[,] blockElements)
    {
        // Don't allow placement while element reactions are still running
        if (IsEffectChainActive) return false;

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
        int cellIndex = 0;
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
                    var cell = cells[boardPos.y, boardPos.x];
                    cell.SetElement(elem, elementData);
                    cell.Normal();

                    // === JUICE: staggered pop-in per cell ===
                    float delay = cellIndex * 0.018f;
                    if (delay > 0f)
                    {
                        // Pass board position so we can abort if cell was cleared during delay
                        StartCoroutine(DelayedPlaceAnim(cell, delay, boardPos.y, boardPos.x));
                    }
                    else
                    {
                        cell.PlayPlaceAnimation();
                    }
                    cellIndex++;
                }
            }
        }
        ClearFullLines(point, polyominoColumns, polyominoRows);
        hoverPoints.Clear();
        SkillManager.Instance.OnBlockPlaced();
    }

    private System.Collections.IEnumerator DelayedPlaceAnim(Cell cell, float delay, int boardR, int boardC)
    {
        yield return new WaitForSeconds(delay);
        // If the cell was cleared while we were waiting, skip the pop-in.
        // Otherwise DelayedPlaceAnim would cancel the FlashAndClear animation.
        if (cell != null && data[boardR, boardC] == 2)
            cell.PlayPlaceAnimation();
    }

    private void ClearFullLines(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        FullLineColumns(point.x, point.x + polyominoColumns);
        FullLineRows(point.y, point.y + polyominoRows);
        ClearFullLinesColumns(); // isClearingLine=true → effects enqueue into pendingClears
        ClearFullLinesRows();

        // ── Buff 5: Clear hàng/cột có 7+ ô liền nhau ──────────
        if (BuffManager.Instance != null && BuffManager.Instance.SevenCellClearEnabled)
            ClearSevenCellRuns();

        // ── Buff 6: Clear đường chéo chính board (đủ 8 ô) ───────
        if (BuffManager.Instance != null && BuffManager.Instance.DiagonalClearEnabled)
            ClearBoardDiagonals();

        // Play sound immediately for the direct line clears
        int totalCleared = fullLineColumns.Count + fullLineRows.Count;
        if (totalCleared > 0)
        {
            SoundManager.Instance?.PlayLineClear(totalCleared);

            // ── Level-mode goal reporting (strictly separated by type) ─────────
            // Each goal type only counts what it says: columns count for ClearColumns,
            // rows count for ClearRows. ClearLines counts ANY line (col or row).
            if (LevelModeManager.Instance != null)
            {
                if (fullLineColumns.Count > 0)
                {
                    LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearColumns, fullLineColumns.Count);
                    LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearLines,   fullLineColumns.Count);
                }
                if (fullLineRows.Count > 0)
                {
                    LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearRows,  fullLineRows.Count);
                    LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearLines, fullLineRows.Count);
                }
            }
        }

        // Process Fire chains, Lightning, and cascade full-lines with visual delays
        StartCoroutine(EffectChainRoutine());
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
            var midWorld = cells[Size / 2, c].transform.position;
            ScoreManager.Instance.AddScore(40);
            ScorePopup.Create(midWorld, 40, UnityEngine.Random.Range(0, 3));
            ScreenShake.Instance?.Shake(0.20f, 0.14f);

            if (ParticleBurst.Instance != null)
            {
                var colColor = cells[0, c].ElementType != Element.Normal
                    ? elementRegistry.GetElementData(cells[0, c].ElementType)?.ElementColor ?? Color.white
                    : Color.white;
                ParticleBurst.Instance.LineClearBurst(midWorld, colColor);
            }

            for (var r = 0; r < Size; ++r)
            {
                cells[r, c].PlayClearAnimation(r * 0.03f);
                if (data[r, c] == 2)
                {
                    var elemType    = elements[r, c];
                    var elementData = elementRegistry.GetElementData(elemType);
                    data[r, c]       = 0;
                    elements[r, c]   = Element.Normal;
                    // isClearingLine=true → Fire's HandleCellClear calls go to pendingClears
                    elementData?.Effect?.ExecuteEffect(this, new Vector2Int(c, r));
                    // Ice may have restored this cell
                    if (data[r, c] != 0) cells[r, c].Normal();
                }
            }
        }
        isClearingLine = false;
    }
    private void ClearFullLinesRows()
    {
        isClearingLine = true;
        foreach (var r in fullLineRows)
        {
            var midWorld = cells[r, Size / 2].transform.position;
            ScoreManager.Instance.AddScore(40);
            ScorePopup.Create(midWorld, 40, UnityEngine.Random.Range(0, 3));
            ScreenShake.Instance?.Shake(0.20f, 0.14f);

            if (ParticleBurst.Instance != null)
            {
                var rowColor = cells[r, 0].ElementType != Element.Normal
                    ? elementRegistry.GetElementData(cells[r, 0].ElementType)?.ElementColor ?? Color.white
                    : Color.white;
                ParticleBurst.Instance.LineClearBurst(midWorld, rowColor);
            }

            for (var c = 0; c < Size; ++c)
            {
                cells[r, c].PlayClearAnimation(c * 0.03f);
                if (data[r, c] == 2)
                {
                    var elemType    = elements[r, c];
                    var elementData = elementRegistry.GetElementData(elemType);
                    data[r, c]       = 0;
                    elements[r, c]   = Element.Normal;
                    elementData?.Effect?.ExecuteEffect(this, new Vector2Int(c, r));
                    if (data[r, c] != 0) cells[r, c].Normal();
                }
            }
        }
        isClearingLine = false;
    }

    // ─────────────────────────────────────────────────────────
    // Fire Delayed Clear (Burn 2s then destroy all at once)
    // ─────────────────────────────────────────────────────────

    public void StartFireDelayedClear(Vector2Int center)
    {
        StartCoroutine(FireDelayedClearRoutine(center));
    }

    private IEnumerator FireDelayedClearRoutine(Vector2Int center)
    {
        activeDelayedEffects++;
        
        List<System.Tuple<Vector2Int, GameObject>> targets = new();
        
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = center.y + dr;
                int nc = center.x + dc;

                if (IsWithinBounds(nc, nr) && data[nr, nc] == 2)
                {
                    var vfxObj = ElementVFX.Instance?.SpawnBurningVFX(GetCellWorldPosition(nc, nr));
                    targets.Add(new (new Vector2Int(nc, nr), vfxObj));
                }
            }
        }

        if (targets.Count > 0)
        {
            // Wait for 1 second while burning
            yield return new WaitForSeconds(1f);
            
            // Explode them ALL AT ONCE!
            SoundManager.Instance?.Play(SoundManager.SFX.FireExplode);
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.35f, 0.20f); // Massive fire shake
            }
            
            foreach (var t in targets)
            {
                if (t.Item2 != null) Destroy(t.Item2); // stop sustained burn
                // Cell clear logic (ExecuteCellClear will internally spawn the FireBurst via ElementVFX)
                ExecuteCellClear(t.Item1.y, t.Item1.x, Element.Fire, null);
            }
            
            if (!isEffectChainActive && pendingClears.Count > 0)
            {
                StartCoroutine(EffectChainRoutine());
            }
        }

        activeDelayedEffects--;
    }

    /// <summary>
    /// Entry point for all element-triggered cell clears.
    /// When called while a line-clear or effect-chain is active, defers to the queue.
    /// Otherwise executes immediately and starts the chain coroutine.
    /// </summary>
    public void HandleCellClear(int r, int c, Element sourceElement = Element.Normal)
    {
        if (data[r, c] != 2) return;

        // Defer: we're inside a line-clear loop or an active chain step → enqueue
        if (isClearingLine || isRunningEffectChain)
        {
            pendingClears.Enqueue(new PendingClear { Position = new Vector2Int(c, r), SourceElement = sourceElement });
            return;
        }

        // Called outside any chain context: execute and let the chain coroutine handle follow-ups
        ExecuteCellClear(r, c, sourceElement, null);
        
        if (!isEffectChainActive)
        {
            StartCoroutine(EffectChainRoutine());
        }
    }

    /// <summary>
    /// Actual clear logic. Sets data=0, plays VFX/sound, executes element effect.
    /// Wraps the effect call in isRunningEffectChain so Fire's recursive calls are deferred.
    /// </summary>
    private void ExecuteCellClear(int r, int c, Element sourceElement = Element.Normal, GameObject vfxHandle = null)
    {
        if (data[r, c] != 2) 
        {
            if (vfxHandle != null) ElementVFX.Instance?.KillPersistentArc(vfxHandle, 0f);
            return;
        }

        if (vfxHandle != null && sourceElement == Element.Lightning)
        {
            // 0.24f matches FlashAndClear length (0.06 flash + 0.18 shrink)
            ElementVFX.Instance?.KillPersistentArc(vfxHandle, 0.24f);
        }

        if (sourceElement == Element.Fire)
        {
            ElementVFX.Instance?.PlayFireVFX(cells[r, c].transform.position);
        }

        if (!isClearingLine) 
        {
            ScoreManager.Instance.AddScore(5);
            ScorePopup.Create(cells[r, c].transform.position, 5, UnityEngine.Random.Range(0, 3));
        }
        var elemType    = elements[r, c];
        var elementData = elementRegistry.GetElementData(elemType);

        data[r, c]     = 0;
        elements[r, c] = Element.Normal;

        if (!isClearingLine)
            SoundManager.Instance?.Play(SoundManager.SFX.CellClear);

        if (ParticleBurst.Instance != null && sourceElement != Element.Fire) // Minimize overlapping effects
            ParticleBurst.Instance.PopBurst(cells[r, c].transform.position,
                elementData?.ElementColor ?? Color.white);
        else if (ParticleBurst.Instance != null && sourceElement == Element.Fire)
        {
             ParticleBurst.Instance.PopBurst(cells[r, c].transform.position,
                 elementData?.ElementColor ?? Color.white);
        }

        cells[r, c].PlayClearAnimation(0f);

        if (elementData?.Effect != null)
        {
            isRunningEffectChain = true;  // Fire's neighbors will be enqueued, not recursed
            elementData.Effect.ExecuteEffect(this, new Vector2Int(c, r));
            isRunningEffectChain = false;
        }

        if (data[r, c] != 0) // Ice may have restored the cell
            cells[r, c].Normal();
    }

    // ─────────────────────────────────────────────────────────
    //  Effect Chain Coroutine
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Processes queued Fire-chain clears, Lightning charges, and cascading full-line
    /// detections one step at a time, with chainStepDelay between each visual step.
    /// This makes reactions readable and satisfying instead of instant.
    /// </summary>
    private IEnumerator EffectChainRoutine()
    {
        isEffectChainActive = true;
        const int safetyLimit = 300; // prevent infinite chains from malformed data
        int steps = 0;

        while (steps < safetyLimit)
        {
            // 1. Check if there's anything to process
            bool hasClears    = pendingClears.Count > 0;
            bool hasNewLines  = false;

            if (!hasClears)
            {
                // Nothing queued; check if fire caused new full lines
                hasNewLines = ClearNewlyFullLines();
                if (!hasNewLines) break; // truly nothing left
            }

            // 2. Wait so the player can see the previous step's animation
            yield return new WaitForSeconds(chainStepDelay);
            steps++;

            // 3. Process one step
            if (pendingClears.Count > 0)
            {
                // Process one deferred Fire / element / lightning clear
                var p = pendingClears.Dequeue();
                ExecuteCellClear(p.Position.y, p.Position.x, p.SourceElement, p.VfxHandle); // may enqueue more into pendingClears
            }
            else
            {
                // New lines were found and cleared by ClearNewlyFullLines above;
                // effects from those clears will be in pendingClears now
                SoundManager.Instance?.PlayLineClear(1);
            }
        }

        isEffectChainActive = false;
    }

    // PickRandomOccupied is now unused because Lightning tracks peeked targets directly


    /// <summary>
    /// Scans the entire board for full rows/columns not already in the current clear lists.
    /// Clears them if found. Returns true if any new lines were cleared.
    /// Effects caused by cells in these lines go to pendingClears for deferred processing.
    /// </summary>
    private bool ClearNewlyFullLines()
    {
        var newCols = new List<int>();
        var newRows = new List<int>();

        for (int c = 0; c < Size; c++)
        {
            if (fullLineColumns.Contains(c)) continue;
            bool full = true;
            for (int r = 0; r < Size; r++)
                if (data[r, c] != 2) { full = false; break; }
            if (full) newCols.Add(c);
        }
        for (int r = 0; r < Size; r++)
        {
            if (fullLineRows.Contains(r)) continue;
            bool full = true;
            for (int c = 0; c < Size; c++)
                if (data[r, c] != 2) { full = false; break; }
            if (full) newRows.Add(r);
        }

        if (newCols.Count == 0 && newRows.Count == 0) return false;

        fullLineColumns.AddRange(newCols);
        fullLineRows.AddRange(newRows);

        if (LevelModeManager.Instance != null)
        {
            if (newCols.Count > 0)
            {
                LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearColumns, newCols.Count);
                LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearLines,   newCols.Count);
            }
            if (newRows.Count > 0)
            {
                LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearRows,  newRows.Count);
                LevelModeManager.Instance.OnGoalProgress(LevelGoalType.ClearLines, newRows.Count);
            }
        }

        isClearingLine = true;
        foreach (int c in newCols)
        {
            var midWorld = cells[Size / 2, c].transform.position;
            ScoreManager.Instance.AddScore(40);
            ScorePopup.Create(midWorld, 40, UnityEngine.Random.Range(0, 3));
            ScreenShake.Instance?.Shake(0.20f, 0.14f);
            ParticleBurst.Instance?.LineClearBurst(cells[Size / 2, c].transform.position, Color.white);
            for (int r = 0; r < Size; r++)
            {
                cells[r, c].PlayClearAnimation(r * 0.03f);
                if (data[r, c] == 2)
                {
                    var ed = elementRegistry.GetElementData(elements[r, c]);
                    data[r, c] = 0; elements[r, c] = Element.Normal;
                    ed?.Effect?.ExecuteEffect(this, new Vector2Int(c, r));
                    if (data[r, c] != 0) cells[r, c].Normal();
                }
            }
        }
        foreach (int r in newRows)
        {
            var midWorld = cells[r, Size / 2].transform.position;
            ScoreManager.Instance.AddScore(40);
            ScorePopup.Create(midWorld, 40, UnityEngine.Random.Range(0, 3));
            ScreenShake.Instance?.Shake(0.20f, 0.14f);
            ParticleBurst.Instance?.LineClearBurst(cells[r, Size / 2].transform.position, Color.white);
            for (int c = 0; c < Size; c++)
            {
                cells[r, c].PlayClearAnimation(c * 0.03f);
                if (data[r, c] == 2)
                {
                    var ed = elementRegistry.GetElementData(elements[r, c]);
                    data[r, c] = 0; elements[r, c] = Element.Normal;
                    ed?.Effect?.ExecuteEffect(this, new Vector2Int(c, r));
                    if (data[r, c] != 0) cells[r, c].Normal();
                }
            }
        }
        isClearingLine = false;

        return true;
    }

    // ─────────────────────────────────────────────────────────
    //  Buff 5: Seven-Cell Run Clear
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Clears any horizontal or vertical run of 7+ consecutive occupied cells.
    /// Uses a 2-pass approach: first collect ALL cells to clear, then clear them.
    /// This prevents horizontal clears from disrupting vertical run detection.
    /// </summary>
    private void ClearSevenCellRuns()
    {
        const int MinRun = 7;

        // ── Pass 1: Collect all cells that belong to a 7+ run ──
        var toClear = new System.Collections.Generic.HashSet<Vector2Int>();

        // Horizontal runs
        for (int r = 0; r < Size; r++)
        {
            int runStart = -1, runLen = 0;
            for (int c = 0; c <= Size; c++)
            {
                bool occupied = c < Size && data[r, c] == 2;
                if (occupied) { if (runLen == 0) runStart = c; runLen++; }
                else
                {
                    if (runLen >= MinRun)
                        for (int cc = runStart; cc < runStart + runLen; cc++)
                            toClear.Add(new Vector2Int(cc, r));
                    runLen = 0; runStart = -1;
                }
            }
        }

        // Vertical runs
        for (int c = 0; c < Size; c++)
        {
            int runStart = -1, runLen = 0;
            for (int r = 0; r <= Size; r++)
            {
                bool occupied = r < Size && data[r, c] == 2;
                if (occupied) { if (runLen == 0) runStart = r; runLen++; }
                else
                {
                    if (runLen >= MinRun)
                        for (int rr = runStart; rr < runStart + runLen; rr++)
                            toClear.Add(new Vector2Int(c, rr));
                    runLen = 0; runStart = -1;
                }
            }
        }

        if (toClear.Count == 0) return;

        // ── Pass 2: Clear all collected cells ──────────────────
        isClearingLine = true;
        int totalScore = 5 * toClear.Count;
        ScoreManager.Instance.AddScore(totalScore);
        
        if (toClear.Count > 0)
        {
            var elem = System.Linq.Enumerable.ElementAt(toClear, toClear.Count / 2);
            var midWorld = cells[elem.y, elem.x].transform.position;
            ScorePopup.Create(midWorld, totalScore, UnityEngine.Random.Range(0, 3));
        }
        
        ScreenShake.Instance?.Shake(0.20f, 0.14f);

        int i = 0;
        foreach (var pos in toClear)
        {
            if (data[pos.y, pos.x] != 2) { i++; continue; }
            var ed = elementRegistry.GetElementData(elements[pos.y, pos.x]);
            cells[pos.y, pos.x].PlayClearAnimation(i * 0.025f);
            data[pos.y, pos.x] = 0; elements[pos.y, pos.x] = Element.Normal;
            ed?.Effect?.ExecuteEffect(this, pos);
            if (data[pos.y, pos.x] != 0) cells[pos.y, pos.x].Normal();
            i++;
        }
        isClearingLine = false;
    }

    // ─────────────────────────────────────────────────────────
    //  Buff 6: Diagonal Clear (2 đường chéo chính của board)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// <summary>
    /// Clears diagonal runs on the board.
    /// Without SevenCellClear buff: only the 2 main full-board diagonals (8 cells).
    /// With SevenCellClear buff: ALL diagonals with 7+ consecutive occupied cells.
    /// </summary>
    private void ClearBoardDiagonals()
    {
        bool sevenCellActive = BuffManager.Instance != null && BuffManager.Instance.SevenCellClearEnabled;
        int minRun = sevenCellActive ? 7 : Size;

        isClearingLine = true;
        ClearDiagonalDirection(+1, minRun);
        ClearDiagonalDirection(-1, minRun);
        isClearingLine = false;
    }

    /// <summary>
    /// Enumerates all diagonals in the given direction that could have length >= minRun,
    /// and tries to clear qualifying runs within each.
    /// </summary>
    private void ClearDiagonalDirection(int colDir, int minRun)
    {
        // Diagonals from the top row (r=0), all columns
        for (int c = 0; c < Size; c++)
        {
            int diagLen = colDir == +1 ? Size - c : c + 1;
            if (diagLen >= minRun)
                TryClearDiagonalRun(0, c, colDir, minRun);
        }
        // Diagonals from the left/right edge column (excluding r=0 — already covered above)
        int edgeCol = colDir == +1 ? 0 : Size - 1;
        for (int r = 1; r < Size; r++)
        {
            int diagLen = Size - r;
            if (diagLen >= minRun)
                TryClearDiagonalRun(r, edgeCol, colDir, minRun);
        }
    }

    /// <summary>
    /// Scans one diagonal for occupied runs >= minRun and clears them.
    /// </summary>
    private void TryClearDiagonalRun(int startRow, int startCol, int colDir, int minRun)
    {
        int diagLen = colDir == +1
            ? Mathf.Min(Size - startRow, Size - startCol)
            : Mathf.Min(Size - startRow, startCol + 1);

        int runStart = -1, runLen = 0;
        for (int i = 0; i <= diagLen; i++) // <= diagLen flushes the last run
        {
            bool occupied = i < diagLen && data[startRow + i, startCol + i * colDir] == 2;
            if (occupied) { if (runLen == 0) runStart = i; runLen++; }
            else
            {
                if (runLen >= minRun)
                    ClearDiagonalSegment(startRow, startCol, colDir, runStart, runLen);
                runLen = 0; runStart = -1;
            }
        }
    }

    private void ClearDiagonalSegment(int startRow, int startCol, int colDir, int segStart, int segLen)
    {
        int totalScore = 40 * segLen / Size + 5 * segLen;
        ScoreManager.Instance.AddScore(totalScore);

        int midIdx = segStart + segLen / 2;
        int midR = startRow + midIdx;
        int midC = startCol + midIdx * colDir;
        if (IsWithinBounds(midC, midR))
        {
            var midWorld = cells[midR, midC].transform.position;
            ScorePopup.Create(midWorld, totalScore, UnityEngine.Random.Range(0, 3));
            if (ParticleBurst.Instance != null)
                ParticleBurst.Instance.LineClearBurst(cells[midR, midC].transform.position, Color.yellow);
        }

        ScreenShake.Instance?.Shake(0.22f, 0.15f);

        for (int i = segStart; i < segStart + segLen; i++)
        {
            int r = startRow + i;
            int c = startCol + i * colDir;
            if (data[r, c] != 2) continue;
            var ed = elementRegistry.GetElementData(elements[r, c]);
            cells[r, c].PlayClearAnimation((i - segStart) * 0.03f);
            data[r, c] = 0; elements[r, c] = Element.Normal;
            ed?.Effect?.ExecuteEffect(this, new Vector2Int(c, r));
            if (data[r, c] != 0) cells[r, c].Normal();
        }
    }

    private void Highlight(Vector2Int point, int polyominoColumns, int polyominoRows)
    {
        PredictFullLineColumns(point.x, point.x + polyominoColumns);
        PredictFullLineRows(point.y, point.y + polyominoRows);
        HighlightFullLinesColumns();
        HighlightFullLinesRows();

        // Buff 5: highlight 7-cell runs that would be cleared on drop
        if (BuffManager.Instance != null && BuffManager.Instance.SevenCellClearEnabled)
            HighlightSevenCellPreview();

        // Buff 6: highlight diagonal cells that would be cleared on drop
        if (BuffManager.Instance != null && BuffManager.Instance.DiagonalClearEnabled)
            HighlightDiagonalPreview();
    }

    /// <summary>
    /// Scans rows and columns (treating hover cells = data 1 as "would-be occupied")
    /// and highlights occupied cells that belong to a run of 7+ consecutive active cells.
    /// </summary>
    private void HighlightSevenCellPreview()
    {
        const int MinRun = 7;
        sevenCellHighlightedCells.Clear();

        // Horizontal
        for (int r = 0; r < Size; r++)
        {
            int runStart = -1, runLen = 0;
            for (int c = 0; c <= Size; c++)
            {
                bool active = c < Size && (data[r, c] == 1 || data[r, c] == 2);
                if (active) { if (runLen == 0) runStart = c; runLen++; }
                else
                {
                    if (runLen >= MinRun)
                        for (int cc = runStart; cc < runStart + runLen; cc++)
                            if (data[r, cc] == 2) { cells[r, cc].Highlight(); sevenCellHighlightedCells.Add(new Vector2Int(cc, r)); }
                    runLen = 0; runStart = -1;
                }
            }
        }

        // Vertical
        for (int c = 0; c < Size; c++)
        {
            int runStart = -1, runLen = 0;
            for (int r = 0; r <= Size; r++)
            {
                bool active = r < Size && (data[r, c] == 1 || data[r, c] == 2);
                if (active) { if (runLen == 0) runStart = r; runLen++; }
                else
                {
                    if (runLen >= MinRun)
                        for (int rr = runStart; rr < runStart + runLen; rr++)
                            if (data[rr, c] == 2) { cells[rr, c].Highlight(); sevenCellHighlightedCells.Add(new Vector2Int(c, rr)); }
                    runLen = 0; runStart = -1;
                }
            }
        }
    }

    private void UnhighlightSevenCellPreview()
    {
        foreach (var pos in sevenCellHighlightedCells)
            if (data[pos.y, pos.x] == 2)
                cells[pos.y, pos.x].Normal();
        sevenCellHighlightedCells.Clear();
    }

    /// <summary>
    /// <summary>
    /// Highlights diagonal cells that would be cleared on drop.
    /// Without SevenCellClear buff: only the 2 main diagonals (need all 8 cells).
    /// With SevenCellClear buff: ALL diagonals with 7+ hover+occupied cells.
    /// </summary>
    private void HighlightDiagonalPreview()
    {
        bool sevenCellActive = BuffManager.Instance != null && BuffManager.Instance.SevenCellClearEnabled;
        int minRun = sevenCellActive ? 7 : Size;
        PreviewDiagonalDirection(+1, minRun);
        PreviewDiagonalDirection(-1, minRun);
    }

    private void PreviewDiagonalDirection(int colDir, int minRun)
    {
        // From top row
        for (int c = 0; c < Size; c++)
        {
            int diagLen = colDir == +1 ? Size - c : c + 1;
            if (diagLen >= minRun)
                PreviewOneDiagonal(0, c, colDir, minRun);
        }
        // From left/right edge (excluding r=0)
        int edgeCol = colDir == +1 ? 0 : Size - 1;
        for (int r = 1; r < Size; r++)
        {
            int diagLen = Size - r;
            if (diagLen >= minRun)
                PreviewOneDiagonal(r, edgeCol, colDir, minRun);
        }
    }

    /// <summary>
    /// Scans one diagonal (hover+occupied = active), highlights occupied cells in qualifying runs.
    /// </summary>
    private void PreviewOneDiagonal(int startRow, int startCol, int colDir, int minRun)
    {
        int diagLen = colDir == +1
            ? Mathf.Min(Size - startRow, Size - startCol)
            : Mathf.Min(Size - startRow, startCol + 1);

        int runStart = -1, runLen = 0;
        for (int i = 0; i <= diagLen; i++)
        {
            bool active = i < diagLen &&
                (data[startRow + i, startCol + i * colDir] == 1 ||
                 data[startRow + i, startCol + i * colDir] == 2);

            if (active) { if (runLen == 0) runStart = i; runLen++; }
            else
            {
                if (runLen >= minRun)
                    for (int j = runStart; j < runStart + runLen; j++)
                    {
                        int r = startRow + j;
                        int c = startCol + j * colDir;
                        if (data[r, c] == 2)
                        {
                            cells[r, c].Highlight();
                            sevenCellHighlightedCells.Add(new Vector2Int(c, r));
                        }
                    }
                runLen = 0; runStart = -1;
            }
        }
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
        UnhighlightSevenCellPreview();
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