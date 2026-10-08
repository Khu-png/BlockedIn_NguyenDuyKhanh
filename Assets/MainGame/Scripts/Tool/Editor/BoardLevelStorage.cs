using System;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardLevelStorage
    {
        const string ParentFolder = "Assets/MainGame/ScriptableObject";
        public const string Folder = ParentFolder + "/Levels";

        public static BoardMap Save(int number, int columns, int rows, bool border, BoardCell[] cells,
            BlockColorData[] blockColors = null, BlockEdgeData[] blockEdges = null, int[] blockGroups = null)
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
            level.columns = columns;
            level.rows = rows;
            level.outerWalls = border;
            level.cells = (BoardCell[])cells.Clone();
            level.blockColors = blockColors == null ? null : (BlockColorData[])blockColors.Clone();
            level.blockEdges = blockEdges == null ? null : (BlockEdgeData[])blockEdges.Clone();
            level.blockGroups = blockGroups == null ? null : (int[])blockGroups.Clone();
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            return level;
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
