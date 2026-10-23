using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class ColorCompletionEvaluator
    {
        static readonly Vector2Int[] connectionOffsets =
            { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        readonly Func<Vector3, Vector2> worldToGrid;
        readonly Dictionary<Vector2Int, Block> positions = new Dictionary<Vector2Int, Block>();
        readonly HashSet<Block> visited = new HashSet<Block>();
        readonly Queue<Block> pending = new Queue<Block>();

        public ColorCompletionEvaluator(Func<Vector3, Vector2> coordinateConverter)
        {
            worldToGrid = coordinateConverter ?? throw new ArgumentNullException(nameof(coordinateConverter));
        }

        public bool IsComplete(IReadOnlyList<Block> group)
        {
            // A completed color needs an actual connection, not a lone plain block.
            if (group.Count < 2) return false;
            positions.Clear();
            for (int index = 0; index < group.Count; index++)
            {
                Block block = group[index];
                if (!StableCell(block)) return false;
                foreach (Vector2Int cell in block.Cells)
                {
                    Vector2Int position = block.GridPosition + cell;
                    if (positions.ContainsKey(position)) return false;
                    positions.Add(position, block);
                }
            }
            return AllPortsMatched(group) && Connected(group);
        }

        bool StableCell(Block block)
        {
            return block != null && block.isActiveAndEnabled && !block.IsFrozen && !block.IsRoped && !block.IsDragging && !block.IsSettling && block.Appearance != null
                && block.HasGoalPosition
                && block.Cells != null && block.Cells.Length > 0
                && Vector2.Distance(worldToGrid(block.transform.position), block.GridPosition) <= BlockBoard.PositionEpsilon;
        }

        bool AllPortsMatched(IReadOnlyList<Block> group)
        {
            for (int index = 0; index < group.Count; index++)
            {
                Block block = group[index];
                foreach (Vector2Int cell in block.Cells)
                    for (int side = 0; side < 4; side++)
                    {
                        EdgeType edge = block.EdgeAt(cell, (BlockSide)side);
                        if (edge != EdgeType.Tab && edge != EdgeType.Socket) continue;
                        if (!TryConnection(block, cell, side, out _)) return false;
                    }
            }
            return true;
        }

        bool TryConnection(Block block, Vector2Int cell, int side, out Block other)
        {
            other = null;
            if (!positions.TryGetValue(block.GridPosition + cell + connectionOffsets[side], out other)) return false;
            EdgeType first = block.EdgeAt(cell, (BlockSide)side);
            if (!BlockConnectionRules.CanMatch(block, other)) return false;
            Vector2Int offset = block.GridPosition + cell + connectionOffsets[side] - other.GridPosition;
            EdgeType second = other.EdgeAt(offset, (BlockSide)((side + 2) % 4));
            return first == EdgeType.Tab && second == EdgeType.Socket
                || first == EdgeType.Socket && second == EdgeType.Tab;
        }

        bool Connected(IReadOnlyList<Block> group)
        {
            visited.Clear();
            pending.Clear();
            visited.Add(group[0]);
            pending.Enqueue(group[0]);
            while (pending.Count > 0)
            {
                Block block = pending.Dequeue();
                foreach (Vector2Int cell in block.Cells)
                for (int side = 0; side < 4; side++)
                    if (TryConnection(block, cell, side, out Block other) && visited.Add(other)) pending.Enqueue(other);
            }
            return visited.Count == group.Count;
        }

    }
}
