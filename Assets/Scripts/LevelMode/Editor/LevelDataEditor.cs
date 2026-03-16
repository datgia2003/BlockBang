using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private LevelData levelData;
    private bool showBoardDesigner = true;
    private bool showSpawnPool = true;

    private void OnEnable()
    {
        levelData = (LevelData)target;
        Polyominos.EnsureBuiltEditor();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Basic info ────────────────────────────────────────────
        EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);
        levelData.LevelName   = EditorGUILayout.TextField("Name",   levelData.LevelName);
        levelData.LevelNumber = EditorGUILayout.IntField("Number",  levelData.LevelNumber);
        EditorGUILayout.Space(6);

        // ── Goals ─────────────────────────────────────────────────
        EditorGUILayout.LabelField("Goals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Goals"), true);
        EditorGUILayout.Space(8);

        serializedObject.ApplyModifiedProperties();

        // ── Move limit ────────────────────────────────────────────
        EditorGUILayout.LabelField("Move Limit", EditorStyles.boldLabel);
        int prev = levelData.MaxBlockPlacements;
        levelData.MaxBlockPlacements = Mathf.Max(0,
            EditorGUILayout.IntField(
                new GUIContent("Max Placements", "0 = use pool total. Player fails if limit reached without completing goals."),
                levelData.MaxBlockPlacements));

        int poolTotal = levelData.TotalPoolSize;
        if (levelData.HasSpawnPool)
        {
            EditorGUILayout.HelpBox(
                $"Pool total: {poolTotal} blocks.  " +
                (levelData.MaxBlockPlacements == 0
                    ? "Move limit will be auto-set to pool total at runtime."
                    : $"Custom limit: {levelData.MaxBlockPlacements}."),
                MessageType.None);
        }

        if (levelData.MaxBlockPlacements != prev) EditorUtility.SetDirty(levelData);
        EditorGUILayout.Space(8);

        // ── Spawn Pool ────────────────────────────────────────────
        showSpawnPool = EditorGUILayout.Foldout(showSpawnPool, "BLOCK SPAWN POOL (Per-Cell Element Design)", true, EditorStyles.foldoutHeader);
        if (showSpawnPool)
        {
            EditorGUILayout.HelpBox(
                "Design exactly which elements appear on which specific cells of each block.\n" +
                "Click a cell in the mini-grid to cycle through element types.",
                MessageType.Info);

            if (levelData.BlockSpawnPool == null)
                levelData.BlockSpawnPool = new List<PolyominoSpawnEntry>();

            int total = Polyominos.Length;

            // Build lookup: index → current entry
            var lookup = new Dictionary<int, PolyominoSpawnEntry>();
            foreach (var e in levelData.BlockSpawnPool)
                if (!lookup.ContainsKey(e.PolyominoIndex))
                    lookup[e.PolyominoIndex] = e;

            int[] tierBoundaries = { 0, 3, 11, 27 };
            string[] tierNames = { "Tier 1 — Small", "Tier 2 — Easy", "Tier 3 — Medium", "Tier 4 — Hard" };
            int tierIdx = 0;

            for (int idx = 0; idx < total; idx++)
            {
                if (tierIdx < tierBoundaries.Length && idx == tierBoundaries[tierIdx])
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField(tierNames[tierIdx], EditorStyles.miniLabel);
                    tierIdx++;
                }

                var poly = Polyominos.Get(idx);
                lookup.TryGetValue(idx, out var entry);
                int count = entry != null ? entry.Count : 0;

                EditorGUILayout.BeginHorizontal();

                // ── Interactive Block Editor ──
                DrawInteractiveBlockEditor(idx, poly, entry);

                // ── Info & Count ──
                GUILayout.Space(8);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Shape #{idx}", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.BeginHorizontal();
                int newCount = EditorGUILayout.IntField(count, GUILayout.Width(40));
                newCount = Mathf.Max(0, newCount);

                if (GUILayout.Button("-", GUILayout.Width(20))) newCount = Mathf.Max(0, newCount - 1);
                if (GUILayout.Button("+", GUILayout.Width(20))) newCount++;
                if (GUILayout.Button("+5", GUILayout.Width(30))) newCount += 5;
                EditorGUILayout.EndHorizontal();

                if (newCount > 0)
                {
                    EditorGUILayout.LabelField("Double-click cells to set elements", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Inactive", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                if (newCount != count)
                {
                    Undo.RecordObject(levelData, "Set Pool Count");
                    if (newCount == 0)
                    {
                        if (entry != null) levelData.BlockSpawnPool.Remove(entry);
                    }
                    else if (entry != null)
                    {
                        entry.Count = newCount;
                    }
                    else
                    {
                        var newEntry = new PolyominoSpawnEntry { PolyominoIndex = idx, Count = newCount };
                        InitializeEntryElements(newEntry);
                        levelData.BlockSpawnPool.Add(newEntry);
                    }
                    EditorUtility.SetDirty(levelData);
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Clear Entire Pool"))
            {
                if (EditorUtility.DisplayDialog("Clear Pool", "Remove all entries?", "Yes", "No"))
                {
                    Undo.RecordObject(levelData, "Clear Spawn Pool");
                    levelData.BlockSpawnPool.Clear();
                    EditorUtility.SetDirty(levelData);
                }
            }
        }

        EditorGUILayout.Space(12);

        // ── Board Designer ────────────────────────────────────────
        showBoardDesigner = EditorGUILayout.Foldout(showBoardDesigner, "BOARD DESIGNER (8×8)", true, EditorStyles.foldoutHeader);
        if (showBoardDesigner)
        {
            if (levelData.InitialBoardData == null || levelData.InitialBoardData.Length != 64)
                levelData.InitialBoardData = new int[64];
            if (levelData.InitialElements == null || levelData.InitialElements.Length != 64)
                levelData.InitialElements = new Element[64];

            float cellSize = 32f;

            for (int r = 7; r >= 0; r--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < 8; c++)
                {
                    int index = r * 8 + c;
                    bool occupied = levelData.InitialBoardData[index] == 2;

                    GUI.backgroundColor = occupied
                        ? GetElementColor(levelData.InitialElements[index])
                        : new Color(0.2f, 0.2f, 0.2f);

                    string label = occupied ? GetElementIcon(levelData.InitialElements[index]) : "·";

                    if (GUILayout.Button(label, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Empty"), !occupied, () => SetBoardCell(index, 0, Element.Normal));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("■ Normal"),    occupied && levelData.InitialElements[index] == Element.Normal, () => SetBoardCell(index, 2, Element.Normal));
                        menu.AddItem(new GUIContent("🔥 Fire"),      levelData.InitialElements[index] == Element.Fire,      () => SetBoardCell(index, 2, Element.Fire));
                        menu.AddItem(new GUIContent("❄ Ice"),       levelData.InitialElements[index] == Element.Ice,       () => SetBoardCell(index, 2, Element.Ice));
                        menu.AddItem(new GUIContent("⚡ Lightning"),  levelData.InitialElements[index] == Element.Lightning, () => SetBoardCell(index, 2, Element.Lightning));
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Clear Board", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Clear Board", "Clear all cells?", "Yes", "No"))
                {
                    Undo.RecordObject(levelData, "Clear Level Board");
                    for (int i = 0; i < 64; i++) { levelData.InitialBoardData[i] = 0; levelData.InitialElements[i] = Element.Normal; }
                    EditorUtility.SetDirty(levelData);
                }
            }
        }
    }

    private void DrawInteractiveBlockEditor(int shapeIdx, int[,] poly, PolyominoSpawnEntry entry)
    {
        int rows = poly.GetLength(0);
        int cols = poly.GetLength(1);
        float cellSize = 14f;

        Rect area = GUILayoutUtility.GetRect(cols * cellSize + 4, rows * cellSize + 4, GUILayout.Width(cols * cellSize + 4));
        GUI.Box(area, "");

        if (entry != null && (entry.CustomElements == null || entry.CustomElements.Length != 25))
            InitializeEntryElements(entry);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (poly[r, c] == 0) continue;

                Rect cellRect = new Rect(area.x + 2 + c * cellSize, area.y + 2 + (rows - 1 - r) * cellSize, cellSize - 1, cellSize - 1);
                
                Element currentElem = entry != null ? entry.CustomElements[r * 5 + c] : Element.Normal;
                Color color = GetElementColor(currentElem);
                
                EditorGUI.DrawRect(cellRect, color);
                
                // Click to cycle elements
                if (entry != null && countOf(entry) > 0)
                {
                    if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                    {
                        Undo.RecordObject(levelData, "Change Block Cell Element");
                        entry.CustomElements[r * 5 + c] = CycleElement(currentElem);
                        EditorUtility.SetDirty(levelData);
                        Event.current.Use();
                    }
                }
            }
        }
    }

    private int countOf(PolyominoSpawnEntry e) => e != null ? e.Count : 0;

    private void InitializeEntryElements(PolyominoSpawnEntry e)
    {
        e.CustomElements = new Element[25];
        for (int i = 0; i < 25; i++) e.CustomElements[i] = Element.Normal;
    }

    private Element CycleElement(Element e) => e switch
    {
        Element.Normal    => Element.Fire,
        Element.Fire      => Element.Ice,
        Element.Ice       => Element.Lightning,
        Element.Lightning => Element.Normal,
        _                 => Element.Normal
    };

    private Color GetElementColor(Element e) => e switch
    {
        Element.Lightning => new Color(1f, 1f, 0.4f),
        Element.Fire      => new Color(1f, 0.4f, 0.3f),
        Element.Ice       => new Color(0.4f, 0.8f, 1f),
        _                 => new Color(0.3f, 0.7f, 0.3f) // Normal green
    };

    private string GetElementIcon(Element e) => e switch
    {
        Element.Fire      => "🔥",
        Element.Ice       => "❄",
        Element.Lightning => "⚡",
        _                 => "■"
    };

    private void SetBoardCell(int index, int data, Element element)
    {
        Undo.RecordObject(levelData, "Modify Level Board");
        levelData.InitialBoardData[index] = data;
        levelData.InitialElements[index]  = element;
        EditorUtility.SetDirty(levelData);
    }
}
