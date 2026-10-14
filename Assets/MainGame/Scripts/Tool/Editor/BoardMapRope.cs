using System;
using System.Collections.Generic;
using BlockedIn.Blocks;
using BlockedIn.Blocks.Editor;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed partial class BoardMapWindow
    {
        [SerializeField] BlockColorData[] ropeColors = Array.Empty<BlockColorData>();
        void EnsureRopes()
        {
            if (ropeColors == null || ropeColors.Length != columns * rows) ropeColors = new BlockColorData[columns * rows];
            for (int i = 0; i < ropeColors.Length; i++) if (blockColors[i] == null) ropeColors[i] = null;
        }
        BlockColorData RopeColor(int index) => ropeColors != null && index < ropeColors.Length ? ropeColors[index] : null;
        BlockColorData DefaultRopeColor(int index)
        {
            foreach (BlockColorData color in blockColors)
                if (color != null && color.ColorId != blockColors[index].ColorId) return color;
            return null;
        }
        void SetRopeColor(int index, BlockColorData color)
        {
            Undo.RecordObject(this, "Change block rope");
            foreach (int cell in BlockShapeLayout.Members(index, blockGroups, blockColors)) ropeColors[cell] = color;
            Repaint();
        }
        void MergeRopes(List<int> members)
        {
            BlockColorData color = null;
            foreach (int index in members)
            {
                if (ropeColors[index] == null) continue;
                if (color != null && color.ColorId != ropeColors[index].ColorId)
                    throw new InvalidOperationException("Select one Rope Color before merging these blocks.");
                color = ropeColors[index];
            }
            foreach (int index in members) ropeColors[index] = color;
        }
    }
}
