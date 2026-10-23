using System;
using System.Collections.Generic;
using BlockedIn.MapTools;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public static class BlockGoalMapping
    {
        public static void Apply(GeneratedBoard layout, IReadOnlyList<Block> blocks, Vector2Int[] goals)
        {
            if (layout == null) throw new InvalidOperationException("Assign a board before configuring solution goals.");
            bool hasMap = goals != null && goals.Length > 0;
            if (hasMap && goals.Length != layout.columns * layout.rows)
                throw new InvalidOperationException("Block goal positions must match the board dimensions.");
            var origins = new Vector2Int[blocks.Count];
            var occupied = new HashSet<Vector2Int>();
            for (int i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i];
                if (block == null) continue;
                origins[i] = ResolveOrigin(layout, block, hasMap ? goals : null);
                ValidateFootprint(layout, block, origins[i], occupied);
            }
            for (int i = 0; i < blocks.Count; i++)
                if (blocks[i] != null) blocks[i].SetGoalPosition(origins[i]);
        }
        static Vector2Int ResolveOrigin(GeneratedBoard layout, Block block, Vector2Int[] goals)
        {
            if (block.Cells == null || block.Cells.Length == 0)
                throw new InvalidOperationException("Each block needs a footprint before assigning solution goals.");
            if (goals == null) return block.GoalPosition;
            Vector2Int origin = new Vector2Int(-1, -1);
            for (int i = 0; i < block.Cells.Length; i++)
            {
                Vector2Int cell = block.GridPosition + block.Cells[i];
                if (!Inside(layout, cell)) throw new InvalidOperationException("Block placement lies outside the board.");
                Vector2Int target = goals[cell.y * layout.columns + cell.x];
                if (target.x < 0 || target.y < 0) throw MissingGoals();
                Vector2Int next = target - block.Cells[i];
                if (i > 0 && next != origin)
                    throw new InvalidOperationException("All cells in a block must share the same solution offset.");
                origin = next;
            }
            return origin;
        }
        static void ValidateFootprint(GeneratedBoard layout, Block block, Vector2Int origin, HashSet<Vector2Int> occupied)
        {
            if (origin.x < 0 || origin.y < 0) throw MissingGoals();
            foreach (Vector2Int offset in block.Cells)
            {
                Vector2Int goal = origin + offset;
                if (!Inside(layout, goal) || layout.cells == null
                    || layout.cells.Length != layout.columns * layout.rows
                    || layout.cells[goal.y * layout.columns + goal.x] != BoardCell.Floor)
                    throw new InvalidOperationException("Every block solution goal must be on a floor cell inside the board.");
                if (!occupied.Add(goal)) throw new InvalidOperationException("Block solution goals cannot overlap.");
            }
        }
        static bool Inside(GeneratedBoard layout, Vector2Int cell) => cell.x >= 0 && cell.y >= 0
            && cell.x < layout.columns && cell.y < layout.rows;
        static InvalidOperationException MissingGoals() => new InvalidOperationException(
            "This level has no complete solution goals. Capture its solved layout in Puzzle Assembly and save the level.");
    }
}
