using System;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed partial class BoardMapWindow : EditorWindow
    {
        [SerializeField] int columns = 4, rows = 4;
        [SerializeField] int levelNumber = 1;
        [SerializeField] BoardCell[] cells;
        [SerializeField] bool outerWalls = true;
        [SerializeField] int brushIndex;
        [SerializeField] BlockColorData[] blockColors;
        [SerializeField] BlockEdgeData[] blockEdges;
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
            EnsureBlocks();
            NormalizeFloor();
            previewCamera = Camera.main;
            if (tilePrefab == null) tilePrefab = BoardDefaultPrefabs.Load("BoardTile");
            if (backdropPrefab == null) backdropPrefab = BoardDefaultPrefabs.Load("BoardBackdropCell");
            if (wallPrefab == null) wallPrefab = BoardDefaultPrefabs.Load("BoardWallCell");
            Undo.undoRedoPerformed += Repaint;
            EnableBoardBinding();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            DisableBoardBinding();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Blocked In — Map Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Paint Floor, Wall or Block on the grid. Click a block to edit its color and edges. Generate applies the layout to the scene.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawSettings();
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160), GUILayout.MaxHeight(500));
                DrawGrid();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.LabelField("Floor: playable area    Wall: obstacle    B: single block    G: merged group", EditorStyles.miniLabel);
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
            var nextBlocks = new BlockColorData[width * height];
            var nextGroups = new int[width * height];
            var nextEdges = new BlockEdgeData[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (blockColors != null && x < columns && y < rows && y * columns + x < blockColors.Length)
                        nextBlocks[y * width + x] = blockColors[y * columns + x];
                    if (blockEdges != null && x < columns && y < rows && y * columns + x < blockEdges.Length)
                        nextEdges[y * width + x] = blockEdges[y * columns + x];
                    if (blockGroups != null && x < columns && y < rows && y * columns + x < blockGroups.Length)
                        nextGroups[y * width + x] = blockGroups[y * columns + x];
                    next[y * width + x] = cells != null && x < columns && y < rows && y * columns + x < cells.Length
                        ? cells[y * columns + x] : BoardCell.Floor;
                }
            blockGroups = nextGroups;
            selectedCells.Clear();
            blockColors = nextBlocks;
            blockEdges = nextEdges;
            columns = width;
            rows = height;
            cells = next;
            RepairGroups();
        }

        void Fill(BoardCell value)
        {
            Undo.RecordObject(this, "Fill map");
            for (int i = 0; i < cells.Length; i++)
            { cells[i] = value; blockColors[i] = null; blockEdges[i] = default; blockGroups[i] = 0; }
            selectedCells.Clear();
        }

        void Example()
        {
            Undo.RecordObject(this, "Example map");
            Resize(6, 6);
            for (int i = 0; i < cells.Length; i++) cells[i] = BoardCell.Floor;
            Array.Clear(blockColors, 0, blockColors.Length);
            Array.Clear(blockEdges, 0, blockEdges.Length);
            Array.Clear(blockGroups, 0, blockGroups.Length);
            selectedCells.Clear();
            outerWalls = true;
        }

        void NormalizeFloor()
        {
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] == BoardCell.Empty) cells[i] = BoardCell.Floor;

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
            DrawShapeTools();
            brushIndex = GUILayout.Toolbar(brushIndex, new[] { "Floor", "Wall", "Block" });
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
            DrawBoardBinding();
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
            RestoreBoardBinding();
            RequireBoardBinding();
            BoardBlockBuilder.Validate(columns, rows, cells, blockColors, blockEdges, blockGroups);
            previewCamera = Camera.main;
            if (tilePrefab == null) tilePrefab = BoardDefaultPrefabs.Load("BoardTile");
            if (backdropPrefab == null) backdropPrefab = BoardDefaultPrefabs.Load("BoardBackdropCell");
            if (wallPrefab == null) wallPrefab = BoardDefaultPrefabs.Load("BoardWallCell");
            GeneratedBoard board = BoardSceneBuilder.Generate(target, columns, rows, outerWalls, cells,
                tilePrefab, backdropPrefab, wallPrefab);
            Undo.RegisterCompleteObjectUndo(this, "Bind generated board");
            SetBoardBinding(board);
            BoardBlockBuilder.Generate(target, blockColors, previewCamera, blockEdges, blockGroups);
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
            RestoreBoardBinding();
            Undo.RegisterCompleteObjectUndo(this, "Clear generated board");
            BoardSceneBuilder.Clear(target);
            SetBoardBinding(null);
        }
    }
}
