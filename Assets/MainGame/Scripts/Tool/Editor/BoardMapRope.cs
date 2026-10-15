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
            var settings = new Dictionary<int, BlockColorData>();
            for (int i = 0; i < ropeColors.Length; i++)
            {
                BlockColorData body = blockColors[i], rope = ropeColors[i];
                if (body == null || rope == null || body.ColorId == rope.ColorId || BoardRopeBuilder.IsObstacle(rope)) continue;
                bool present = Array.Exists(blockColors, color => color != null && color.ColorId == rope.ColorId);
                if (present && !settings.ContainsKey(body.ColorId)) settings[body.ColorId] = rope;
            }
            for (int i = 0; i < ropeColors.Length; i++)
                ropeColors[i] = blockColors[i] != null && settings.TryGetValue(blockColors[i].ColorId, out BlockColorData rope) ? rope : null;
        }
        BlockColorData RopeColor(int index) => ropeColors != null && index < ropeColors.Length ? ropeColors[index] : null;
        BlockColorData[] AvailableRopeColors(int index)
        {
            if (index < 0 || index >= blockColors.Length || blockColors[index] == null) return Array.Empty<BlockColorData>();
            var result = new List<BlockColorData>(); var ids = new HashSet<int>();
            foreach (BlockColorData color in blockColors)
                if (color != null && color.Material != null && color.ColorId != blockColors[index].ColorId
                    && !BoardRopeBuilder.IsObstacle(color) && ids.Add(color.ColorId)) result.Add(color);
            return result.ToArray();
        }
        void SetRopeColor(int index, BlockColorData color)
        {
            Undo.RecordObject(this, "Change block rope");
            int id = blockColors[index].ColorId;
            for (int i = 0; i < blockColors.Length; i++)
                if (blockColors[i] != null && blockColors[i].ColorId == id) ropeColors[i] = color;
            Repaint();
        }
        BlockColorData InheritedRope(int index, BlockColorData color)
        {
            if (color == null) return null;
            List<int> members = BlockShapeLayout.Members(index, blockGroups, blockColors);
            for (int i = 0; i < blockColors.Length; i++)
                if (!members.Contains(i) && blockColors[i] != null && blockColors[i].ColorId == color.ColorId)
                    return ropeColors[i];
            BlockColorData previous = RopeColor(index);
            return previous != null && previous.ColorId != color.ColorId ? previous : null;
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
