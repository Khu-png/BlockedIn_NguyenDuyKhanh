using System.Collections.Generic;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class PuzzlePieceSwap
    {
        public static bool TryPlan(int columns, BoardCell[] floor, BlockColorData[] colors, int[] groups,
            List<int> piece, Vector2Int delta, out List<int> other)
        {
            other = null;
            var destination = new HashSet<int>();
            foreach (int cell in piece)
            {
                if (!TryDestination(cell, delta, columns, floor, out int next)) return false;
                destination.Add(next);
                if (colors[next] == null || piece.Contains(next)) continue;
                if (other == null) other = BlockShapeLayout.Members(next, groups, colors);
                else if (!other.Contains(next)) return false;
            }
            if (other == null) return false;
            foreach (int cell in other)
            {
                if (!TryDestination(cell, -delta, columns, floor, out int next) || destination.Contains(next)) return false;
                if (colors[next] != null && !piece.Contains(next) && !other.Contains(next)) return false;
            }
            return true;
        }

        static bool TryDestination(int cell, Vector2Int delta, int columns, BoardCell[] floor, out int next)
        {
            int x = cell % columns + delta.x, y = cell / columns + delta.y;
            next = y * columns + x;
            return x >= 0 && x < columns && y >= 0 && y < floor.Length / columns && floor[next] == BoardCell.Floor;
        }

        public static List<int> Apply(int columns, BlockColorData[] colors, BlockEdgeData[] edges, int[] groups,
            BlockColorData[] ropes, Vector2Int[] goalPositions, List<int> piece, List<int> other, Vector2Int delta)
        {
            var oldColors = (BlockColorData[])colors.Clone();
            var oldEdges = (BlockEdgeData[])edges.Clone();
            var oldGroups = (int[])groups.Clone();
            var oldRopes = (BlockColorData[])ropes.Clone();
            var oldGoals = (Vector2Int[])goalPositions.Clone();
            var sources = new List<int>(piece);sources.AddRange(other);
            foreach (int cell in sources)
            {
                colors[cell] = null;edges[cell] = default;groups[cell] = 0;ropes[cell] = null;
                goalPositions[cell] = PuzzlePieceMover.InvalidGoal;
            }
            var moved = new List<int>();
            foreach (int cell in sources)
            {
                bool first = piece.Contains(cell);
                int offset = delta.x + delta.y * columns;
                int next = cell + (first ? offset : -offset);
                colors[next] = oldColors[cell];edges[next] = oldEdges[cell];
                groups[next] = oldGroups[cell];ropes[next] = oldRopes[cell];
                goalPositions[next] = oldGoals[cell];
                if (first) moved.Add(next);
            }
            return moved;
        }
    }
}
