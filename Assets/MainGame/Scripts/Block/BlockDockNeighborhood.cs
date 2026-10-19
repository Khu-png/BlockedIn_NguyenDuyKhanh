using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockDockNeighborhood
    {
        static readonly Vector2Int[] offsets =
            { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        readonly HashSet<Vector2Int> adjacentCells = new HashSet<Vector2Int>();

        public void Refresh(Block changed, IReadOnlyList<Block> blocks)
        {
            if (changed == null) return;
            adjacentCells.Clear();
            foreach (Vector2Int cell in changed.Cells)
                foreach (Vector2Int offset in offsets)
                    adjacentCells.Add(changed.GridPosition + cell + offset);
            foreach (Block block in blocks)
            {
                if (block == null) continue;
                if (block == changed || changed.HasSameColor(block) && Touches(block))
                    block.RefreshDockPorts();
            }
        }
        bool Touches(Block block)
        {
            foreach (Vector2Int cell in block.Cells)
                if (adjacentCells.Contains(block.GridPosition + cell)) return true;
            return false;
        }
    }
}
