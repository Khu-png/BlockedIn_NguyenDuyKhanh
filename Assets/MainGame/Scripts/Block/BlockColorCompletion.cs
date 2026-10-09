using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        static readonly Vector2Int[] connectionOffsets = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        bool completionPending;

        public void RequestCompletionCheck() => completionPending = true;

        void LateUpdate()
        {
            if (!completionPending) return;
            foreach (Block block in blocks)
                if (block != null && (block.IsDragging || block.IsSettling)) return;
            completionPending = false;
            CheckCompletedColors();
        }

        public void CheckCompletedColors()
        {
            if (!Application.isPlaying) return;
            var colorIds = new HashSet<int>();
            foreach (Block block in blocks)
                if (block != null && block.ColorData != null) colorIds.Add(block.ColorData.ColorId);
            foreach (int colorId in colorIds)
            {
                List<Block> group = ColorGroup(colorId);
                if (!IsColorComplete(group)) continue;
                RemoveColor(group);
            }
        }

        List<Block> ColorGroup(int colorId)
        {
            var group = new List<Block>();
            foreach (Block block in blocks)
                if (block != null && block.ColorData != null && block.ColorData.ColorId == colorId) group.Add(block);
            return group;
        }

        bool IsColorComplete(List<Block> group)
        {
            // A completed color needs an actual connection, not a lone plain block.
            if (group.Count < 2) return false;
            var positions = new Dictionary<Vector2Int, Block>();
            foreach (Block block in group)
            {
                if (!StableCell(block)) return false;
                foreach (Vector2Int cell in block.Cells)
                {
                    Vector2Int position = block.GridPosition + cell;
                    if (positions.ContainsKey(position)) return false;
                    positions.Add(position, block);
                }
            }
            return AllPortsMatched(group, positions) && Connected(group, positions);
        }

        bool StableCell(Block block)
        {
            return block.isActiveAndEnabled && !block.IsDragging && !block.IsSettling && block.Appearance != null
                && block.Cells != null && block.Cells.Length > 0
                && Vector2.Distance(WorldToGrid(block.transform.position), block.GridPosition) <= PositionEpsilon;
        }

        static bool AllPortsMatched(List<Block> group, Dictionary<Vector2Int, Block> positions)
        {
            foreach (Block block in group)
                foreach (Vector2Int cell in block.Cells)
                for (int side = 0; side < 4; side++)
                {
                    EdgeType edge = block.EdgeAt(cell, (BlockSide)side);
                    if (edge != EdgeType.Tab && edge != EdgeType.Socket) continue;
                    if (!TryConnection(block, cell, side, positions, out _)) return false;
                }
            return true;
        }

        static bool TryConnection(Block block, Vector2Int cell, int side, Dictionary<Vector2Int, Block> positions, out Block other)
        {
            other = null;
            if (!positions.TryGetValue(block.GridPosition + cell + connectionOffsets[side], out other)) return false;
            EdgeType first = block.EdgeAt(cell, (BlockSide)side);
            if (other == block) return false;
            Vector2Int offset = block.GridPosition + cell + connectionOffsets[side] - other.GridPosition;
            EdgeType second = other.EdgeAt(offset, (BlockSide)((side + 2) % 4));
            return first == EdgeType.Tab && second == EdgeType.Socket
                || first == EdgeType.Socket && second == EdgeType.Tab;
        }

        static bool Connected(List<Block> group, Dictionary<Vector2Int, Block> positions)
        {
            var visited = new HashSet<Block> { group[0] };
            var pending = new Queue<Block>();
            pending.Enqueue(group[0]);
            while (pending.Count > 0)
            {
                Block block = pending.Dequeue();
                foreach (Vector2Int cell in block.Cells)
                for (int side = 0; side < 4; side++)
                    if (TryConnection(block, cell, side, positions, out Block other) && visited.Add(other)) pending.Enqueue(other);
            }
            return visited.Count == group.Count;
        }

    }
}



namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        public event Action ColorsRemoved;
        public bool HasRemainingGoals
        {
            get
            {
                foreach (Block block in blocks)
                    if (HasPorts(block)) return true;
                return false;
            }
        }

        static bool HasPorts(Block block)
        {
            if (block == null) return false;
            foreach (var cell in block.Cells)
                for (int side = 0; side < 4; side++)
                {
                    EdgeType edge = block.EdgeAt(cell, (BlockSide)side);
                    if (edge == EdgeType.Tab || edge == EdgeType.Socket) return true;
                }
            return false;
        }
    }
}
