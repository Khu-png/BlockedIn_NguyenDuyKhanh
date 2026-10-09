using System;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    [InitializeOnLoad]
    public static class BoardLevelStorage
    {
        const string ParentFolder = "Assets/MainGame/ScriptableObject";
        public const string Folder = ParentFolder + "/Levels";

        static BoardLevelStorage()
        {
            EditorApplication.delayCall += BuildMissingRuntimeBoards;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += BuildMissingRuntimeBoards;
        }

        public static void BuildMissingRuntimeBoards()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !System.IO.Directory.Exists(Folder)
                || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) return;
            bool changed = false;
            foreach (string file in System.IO.Directory.GetFiles(Folder, "*.asset"))
            {
                BoardMap level = AssetDatabase.LoadAssetAtPath<BoardMap>(file.Replace('\\', '/'));
                if (level == null || level.RuntimeBoard != null) continue;
                try
                {
                    BoardBlockBuilder.Validate(level.columns, level.rows, level.cells,
                        level.blockColors, level.blockEdges, level.blockGroups);
                    BakeRuntimeBoard(level);
                    changed = true;
                }
                catch (Exception exception) { Debug.LogError("Could not prepare " + level.name + ": " + exception.Message); }
            }
            if (!changed) return;
            AssetDatabase.SaveAssets();
            LevelManager.RefreshRegisteredLevels();
        }

        public static BoardMap Save(int number, int columns, int rows, bool border, BoardCell[] cells,
            BlockColorData[] blockColors = null, BlockEdgeData[] blockEdges = null, int[] blockGroups = null,
            GameObject tile = null, GameObject backdrop = null, GameObject wall = null, float? timeLimit = null)
        {
            Validate(number, columns, rows, cells);
            BoardBlockBuilder.Validate(columns, rows, cells, blockColors, blockEdges, blockGroups);
            if (!AssetDatabase.IsValidFolder(ParentFolder)) AssetDatabase.CreateFolder("Assets/MainGame", "ScriptableObject");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(ParentFolder, "Levels");
            string name = "Level " + number;
            string path = Folder + "/" + name + ".asset";
            var level = AssetDatabase.LoadAssetAtPath<BoardMap>(path);
            if (level == null) level = Create(path);
            Undo.RecordObject(level, "Save " + name);
            level.name = name;
            level.levelNumber = number;
            if (timeLimit.HasValue) level.timeLimit = Mathf.Max(1, timeLimit.Value);
            level.columns = columns;
            level.rows = rows;
            level.outerWalls = border;
            level.cells = (BoardCell[])cells.Clone();
            level.blockColors = blockColors == null ? null : (BlockColorData[])blockColors.Clone();
            level.blockEdges = blockEdges == null ? null : (BlockEdgeData[])blockEdges.Clone();
            level.blockGroups = blockGroups == null ? null : (int[])blockGroups.Clone();
            BakeRuntimeBoard(level, tile, backdrop, wall);
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            LevelManager.RefreshRegisteredLevels();
            return level;
        }

        public static void BakeRuntimeBoard(BoardMap level, GameObject tile = null,
            GameObject backdrop = null, GameObject wall = null)
        {
            const string prefabFolder = "Assets/MainGame/Prefabs/Levels";
            if (!AssetDatabase.IsValidFolder(prefabFolder)) AssetDatabase.CreateFolder("Assets/MainGame/Prefabs", "Levels");
            GeneratedBoard board = null;
            UnityEngine.Object previousSelection = Selection.activeObject;
            try
            {
                board = BoardSceneBuilder.Generate(null, level.columns, level.rows, level.outerWalls, level.cells,
                    tile != null ? tile : BoardDefaultPrefabs.Load("BoardTile"),
                    backdrop != null ? backdrop : BoardDefaultPrefabs.Load("BoardBackdropCell"),
                    wall != null ? wall : BoardDefaultPrefabs.Load("BoardWallCell"));
                BoardBlockBuilder.Generate(board, level.blockColors, null, level.blockEdges, level.blockGroups);
                string path = prefabFolder + "/Level " + level.levelNumber + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(board.gameObject, path, out bool success);
                if (!success) throw new InvalidOperationException("Could not save the runtime level prefab.");
                GeneratedBoard saved = null;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is GeneratedBoard root) { saved = root; break; }
                if (saved == null) throw new InvalidOperationException("The runtime level prefab has no board reference.");
                level.SetRuntimeBoard(saved);
                EditorUtility.SetDirty(level);
            }
            finally
            {
                if (board != null) UnityEngine.Object.DestroyImmediate(board.gameObject);
                Selection.activeObject = previousSelection;
            }
        }

        static BoardMap Create(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("The level path already contains a different asset type: " + path);
            var level = ScriptableObject.CreateInstance<BoardMap>();
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        static void Validate(int number, int columns, int rows, BoardCell[] cells)
        {
            if (number < 1) throw new ArgumentException("Level number must be at least 1.");
            if (columns < 1 || columns > 32 || rows < 1 || rows > 32 || cells == null || cells.Length != columns * rows)
                throw new ArgumentException("Invalid board dimensions or cell data.");
        }
    }
}
