using System;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed partial class BoardMapWindow
    {
        void SaveAsLevel()
        {
            mapAsset = BoardLevelStorage.Save(levelNumber, columns, rows, outerWalls, cells, blockColors, blockEdges, blockGroups);
            EditorGUIUtility.PingObject(mapAsset);
        }

        void SaveTo(BoardMap asset)
        {
            BoardBlockBuilder.Validate(columns, rows, cells, blockColors, blockEdges, blockGroups);
            Undo.RecordObject(asset, "Save board map");
            asset.columns = columns;
            asset.rows = rows;
            asset.outerWalls = outerWalls;
            asset.cells = (BoardCell[])cells.Clone();
            asset.blockColors = (BlockColorData[])blockColors.Clone();
            asset.blockEdges = (BlockEdgeData[])blockEdges.Clone();
            asset.blockGroups = (int[])blockGroups.Clone();
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
            blockColors = mapAsset.blockColors == null ? null : (BlockColorData[])mapAsset.blockColors.Clone();
            blockEdges = mapAsset.blockEdges == null ? null : (BlockEdgeData[])mapAsset.blockEdges.Clone();
            blockGroups = mapAsset.blockGroups == null ? null : (int[])mapAsset.blockGroups.Clone();
            selectedCells.Clear();
            EnsureBlocks();
            levelNumber = Mathf.Max(1, mapAsset.levelNumber);
            NormalizeFloor();
        }

    }
}
