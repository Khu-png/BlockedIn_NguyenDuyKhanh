using System;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed class BoardMapWindow : EditorWindow
    {
        [SerializeField] int columns = 4, rows = 4;
        [SerializeField] int levelNumber = 1;
        [SerializeField] BoardCell[] cells;
        [SerializeField] bool outerWalls = true;
        [SerializeField] BoardCell brush = BoardCell.Floor;
        [SerializeField] BoardMap mapAsset;
        [SerializeField] GeneratedBoard target;
        [SerializeField] GameObject tilePrefab, backdropPrefab, wallPrefab;
        Camera previewCamera;
        Vector2 scroll;
        bool painting;
        int paintUndoGroup;

        [MenuItem("Tools/Blocked In/Map Generator")]
        public static void Open() => GetWindow<BoardMapWindow>("Map Generator");

        void OnEnable()
        {
            minSize = new Vector2(390, 680);
            if (cells == null || cells.Length != columns * rows) Resize(columns, rows);
            NormalizeFloor();
            previewCamera = Camera.main;
            if (tilePrefab == null) tilePrefab = BoardDefaultPrefabs.Load("BoardTile");
            if (backdropPrefab == null) backdropPrefab = BoardDefaultPrefabs.Load("BoardBackdropCell");
            if (wallPrefab == null) wallPrefab = BoardDefaultPrefabs.Load("BoardWallCell");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Blocked In — Map Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a brush and drag across the grid. The top row points toward +Z. One cell equals 1 Unity unit.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawSettings();
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160), GUILayout.MaxHeight(500));
                DrawGrid();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.LabelField("Floor: playable area    Wall: obstacle", EditorStyles.miniLabel);
                DrawStorage();
                EditorGUILayout.Space();
                DrawGeneration();
                DrawPreview();

            }
        }

        void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { Debug.LogException(exception); EditorUtility.DisplayDialog("Map Generator", exception.Message, "OK"); }
        }

        void Resize(int width, int height)
        {
            var next = new BoardCell[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    next[y * width + x] = cells != null && x < columns && y < rows && y * columns + x < cells.Length
                        ? cells[y * columns + x] : BoardCell.Floor;
            columns = width;
            rows = height;
            cells = next;
        }

        void Fill(BoardCell value)
        {
            Undo.RecordObject(this, "Fill map");
            for (int i = 0; i < cells.Length; i++) cells[i] = value;
        }

        void Example()
        {
            Undo.RecordObject(this, "Example map");
            Resize(6, 6);
            for (int i = 0; i < cells.Length; i++) cells[i] = BoardCell.Floor;
            outerWalls = true;
        }

        void DrawGrid()
        {
            const float size = 30;
            Rect grid = GUILayoutUtility.GetRect(columns * size, rows * size, GUILayout.ExpandWidth(false));
            PaintGrid(grid, size);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    BoardCell cell = cells[y * columns + x];
                    Rect rect = new Rect(grid.x + x * size, grid.y + y * size, size - 2, size - 2);
                    EditorGUI.DrawRect(rect, cell == BoardCell.Floor ? new Color(.20f, .52f, .65f)
                        : cell == BoardCell.Wall ? new Color(.85f, .68f, .28f) : new Color(.18f, .18f, .18f));
                    GUI.Label(rect, cell == BoardCell.Wall ? "W" : cell == BoardCell.Floor ? "·" : "", EditorStyles.centeredGreyMiniLabel);
                }
        }

        void PaintGrid(Rect grid, float size)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && grid.Contains(e.mousePosition))
            {
                painting = true;
                Undo.IncrementCurrentGroup();
                paintUndoGroup = Undo.GetCurrentGroup();
                Undo.RecordObject(this, "Paint map");
            }
            if (painting && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && grid.Contains(e.mousePosition))
            {
                int x = Mathf.FloorToInt((e.mousePosition.x - grid.x) / size);
                int y = Mathf.FloorToInt((e.mousePosition.y - grid.y) / size);
                Undo.RecordObject(this, "Paint map");
                cells[y * columns + x] = brush;
                e.Use(); Repaint();
            }
            if (painting && (e.rawType == EventType.MouseUp || e.type == EventType.Ignore))
            {
                painting = false;
                Undo.CollapseUndoOperations(paintUndoGroup);
            }
        }

        void SaveAsLevel()
        {
            mapAsset = BoardLevelStorage.Save(levelNumber, columns, rows, outerWalls, cells);
            EditorGUIUtility.PingObject(mapAsset);
        }

        void SaveTo(BoardMap asset)
        {
            Undo.RecordObject(asset, "Save board map");
            asset.columns = columns;
            asset.rows = rows;
            asset.outerWalls = outerWalls;
            asset.cells = (BoardCell[])cells.Clone();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        void Load()
        {
            if (!mapAsset.IsValid) throw new InvalidOperationException("Invalid map data: dimensions must be 1 to 32 and cell count must equal rows * columns.");
            Undo.RecordObject(this, "Load board map");
            columns = mapAsset.columns;
            rows = mapAsset.rows;
            outerWalls = mapAsset.outerWalls;
            cells = (BoardCell[])mapAsset.cells.Clone();
            levelNumber = Mathf.Max(1, mapAsset.levelNumber);
            NormalizeFloor();
        }

        void NormalizeFloor()
        {
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] == BoardCell.Empty) cells[i] = BoardCell.Floor;
            if (brush == BoardCell.Empty) brush = BoardCell.Floor;
        }

        void DrawSettings()
        {
            EditorGUI.BeginChangeCheck();
            int newColumns = EditorGUILayout.IntSlider("Columns", columns, 1, 32);
            int newRows = EditorGUILayout.IntSlider("Rows", rows, 1, 32);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Resize map");
                Resize(newColumns, newRows);
        }
        outerWalls = EditorGUILayout.Toggle("Outer wall border", outerWalls);
        int selectedBrush = GUILayout.Toolbar(brush == BoardCell.Wall ? 1 : 0, new[] { "Floor", "Wall" });
        brush = selectedBrush == 1 ? BoardCell.Wall : BoardCell.Floor;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Fill Floor")) Fill(BoardCell.Floor);
                if (GUILayout.Button("Example 6×6")) Example();
        }

        }

        void DrawStorage()
        {
            mapAsset = (BoardMap)EditorGUILayout.ObjectField("Map data", mapAsset, typeof(BoardMap), false);
            levelNumber = Mathf.Max(1, EditorGUILayout.IntField("Level number", levelNumber));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save as Level")) Run(SaveAsLevel);
                using (new EditorGUI.DisabledScope(mapAsset == null))
                {
                    if (GUILayout.Button("Save")) Run(() => SaveTo(mapAsset));
                    if (GUILayout.Button("Load")) Run(Load);
                }
        }
        }

        void DrawGeneration()
        {
            tilePrefab = PrefabField("Tile prefab", tilePrefab);
            backdropPrefab = PrefabField("Backdrop prefab", backdropPrefab);
            wallPrefab = PrefabField("Wall prefab", wallPrefab);
            target = (GeneratedBoard)EditorGUILayout.ObjectField("Generated board", target, typeof(GeneratedBoard), true);
            EditorGUILayout.HelpBox("Generate updates the selected board or creates a new board if none is assigned. Clear removes only the selected board. Undo is supported.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate", GUILayout.Height(32))) Run(Generate);
                using (new EditorGUI.DisabledScope(target == null))
                    if (GUILayout.Button("Clear", GUILayout.Height(32))) Run(Clear);
            }
        }

        static GameObject PrefabField(string label, GameObject prefab)
        {
            return (GameObject)EditorGUILayout.ObjectField(label, prefab, typeof(GameObject), false);
        }

        void Generate()
        {
            if (tilePrefab == null) tilePrefab = BoardDefaultPrefabs.Load("BoardTile");
            if (backdropPrefab == null) backdropPrefab = BoardDefaultPrefabs.Load("BoardBackdropCell");
            if (wallPrefab == null) wallPrefab = BoardDefaultPrefabs.Load("BoardWallCell");
            target = BoardSceneBuilder.Generate(target, columns, rows, outerWalls, cells,
                tilePrefab, backdropPrefab, wallPrefab);
        }

        void DrawPreview()
        {
            previewCamera = Camera.main;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Preview camera", previewCamera, typeof(Camera), true);
            if (previewCamera == null) EditorGUILayout.HelpBox("Enable a camera tagged MainCamera to use preview.", MessageType.Info);
            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Frame in Scene")) BoardPreview.FrameScene(target);
                using (new EditorGUI.DisabledScope(previewCamera == null))
                    if (GUILayout.Button("Fit Camera / View in Game")) Run(() => BoardPreview.FitCamera(target, previewCamera));
            }
        }

        void Clear()
        {
            BoardSceneBuilder.Clear(target);
            target = null;
        }
    }
}
