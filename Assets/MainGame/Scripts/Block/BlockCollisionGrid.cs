using System;
using System.Collections.Generic;
using BlockedIn.MapTools;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockCollisionGrid
    {
        bool[] blocked = Array.Empty<bool>();
        int columns, rows;

        public void Prepare(GeneratedBoard layout, IReadOnlyList<Block> blocks, Block moving)
        {
            columns = layout != null ? layout.columns : 0;
            rows = layout != null ? layout.rows : 0;
            if (layout == null) return;
            int count = columns * rows;
            if (layout.cells == null || layout.cells.Length != count)
                throw new InvalidOperationException("Board cell data does not match its dimensions.");
            if (blocked.Length != count) blocked = new bool[count];
            for (int i = 0; i < count; i++) blocked[i] = layout.cells[i] != BoardCell.Floor;
            for (int i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i];
                if (block == null || block == moving) continue;
                foreach (Vector2Int offset in block.Cells)
                {
                    Vector2Int cell = block.GridPosition + offset;
                    if (Contains(cell)) blocked[cell.y * columns + cell.x] = true;
                }
            }
        }
        bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < columns && cell.y < rows;
        public bool IsBlocked(Vector2Int cell) => !Contains(cell) || blocked[cell.y * columns + cell.x];
    }
}
