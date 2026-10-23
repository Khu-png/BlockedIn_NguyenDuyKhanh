using System;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardGoalValidator
    {
        public static void Validate(int columns, int rows, BoardCell[] cells,
            BlockColorData[] colors, int[] groups, Vector2Int[] goals)
        {
            bool hasBlocks = false;
            if (colors != null) foreach (BlockColorData color in colors) if (color != null) { hasBlocks = true; break; }
            if (!hasBlocks) return;
            if (goals == null || goals.Length == 0)
                throw new InvalidOperationException("Capture the solved layout in Puzzle Assembly before saving this level.");
            if (goals.Length != columns * rows)
                throw new InvalidOperationException("Block goal positions must match the grid dimensions.");
            if (cells == null || cells.Length != goals.Length)
                throw new InvalidOperationException("Board cell data must match the goal grid.");
            bool foundGoal = false, foundUnsetGoal = false;
            var occupied = new System.Collections.Generic.HashSet<Vector2Int>();
            for (int i = 0; i < goals.Length; i++)
            {
                if (colors == null || colors[i] == null) continue;
                Vector2Int goal = goals[i];
                if (goal == PuzzlePieceMover.InvalidGoal) { foundUnsetGoal = true; continue; }
                foundGoal = true;
                if (goal.x < 0 || goal.x >= columns || goal.y < 0 || goal.y >= rows)
                    throw new InvalidOperationException("Block goal positions must stay inside the board.");
                int goalIndex = goal.y * columns + goal.x;
                if (cells[goalIndex] != BoardCell.Floor)
                    throw new InvalidOperationException("Block goals must use floor cells.");
                if (!occupied.Add(goal))
                    throw new InvalidOperationException("Two block cells cannot share a goal position.");
            }
            if (foundUnsetGoal)
                throw new InvalidOperationException("Assign a goal position to every block cell before generating the board.");
            if (!foundGoal || groups == null || groups.Length != colors.Length) return;
            var checkedGroups = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null || groups[i] <= 0 || !checkedGroups.Add(groups[i])) continue;
                var members = BlockShapeLayout.Members(i, groups, colors);
                Vector2Int offset = goals[members[0]] - new Vector2Int(members[0] % columns, members[0] / columns);
                foreach (int cell in members)
                    if (goals[cell] - new Vector2Int(cell % columns, cell / columns) != offset)
                        throw new InvalidOperationException("All cells in a block shape must share the same goal offset.");
            }
        }
    }
}
