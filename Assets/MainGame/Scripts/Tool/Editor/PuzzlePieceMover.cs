using System.Collections.Generic;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEngine;
namespace BlockedIn.EditorTools
{
    public static class PuzzlePieceMover
    {
        public static bool CanPlace(int columns, BoardCell[] floor, BlockColorData[] colors, List<int> piece, Vector2Int delta)
        {
            foreach (int cell in piece)
            {
                int x = cell % columns + delta.x, y = cell / columns + delta.y;
                if (x < 0 || x >= columns || y < 0 || y >= floor.Length / columns) return false;
                int next = y * columns + x;
                if (floor[next] != BoardCell.Floor || colors[next] != null && !piece.Contains(next)) return false;
            }
            return true;
        }
        public static bool CanSlide(int columns, BoardCell[] floor, BlockColorData[] colors, List<int> piece, Vector2Int delta)
        {
            return ClearPath(columns, floor, colors, piece, delta, true) || ClearPath(columns, floor, colors, piece, delta, false);
        }
        static bool ClearPath(int columns, BoardCell[] floor, BlockColorData[] colors, List<int> piece, Vector2Int delta, bool horizontalFirst)
        {
            Vector2Int current = Vector2Int.zero;
            for (int pass = 0; pass < 2; pass++)
            {
                bool horizontal = pass == 0 ? horizontalFirst : !horizontalFirst;
                int distance = horizontal ? delta.x : delta.y;
                Vector2Int step = horizontal ? Vector2Int.right : Vector2Int.up;
                step *= distance < 0 ? -1 : 1;
                for (int i = 0; i < Mathf.Abs(distance); i++)
                {
                    current += step;
                    if (!CanPlace(columns, floor, colors, piece, current)) return false;
                }
            }
            return true;
        }
        public static List<int> Move(int columns, BlockColorData[] colors, BlockEdgeData[] edges, int[] groups,
            BlockColorData[] ropes, Vector2Int[] goalPositions, List<int> piece, Vector2Int delta)
        {
            var savedColors = new BlockColorData[piece.Count];var savedEdges = new BlockEdgeData[piece.Count];
            var savedGroups = new int[piece.Count];var savedRopes = new BlockColorData[piece.Count];
            var savedGoals = new Vector2Int[piece.Count];
            for (int i = 0; i < piece.Count; i++)
            {
                int cell = piece[i];savedColors[i] = colors[cell];savedEdges[i] = edges[cell];
                savedGroups[i] = groups[cell];savedRopes[i] = ropes[cell];savedGoals[i] = goalPositions[cell];
                colors[cell] = null;edges[cell] = default;groups[cell] = 0;ropes[cell] = null;
                goalPositions[cell] = InvalidGoal;
            }
            var moved = new List<int>();
            for (int i = 0; i < piece.Count; i++)
            {
                int cell = piece[i] + delta.x + delta.y * columns;
                colors[cell] = savedColors[i];edges[cell] = savedEdges[i];groups[cell] = savedGroups[i];
                ropes[cell] = savedRopes[i];goalPositions[cell] = savedGoals[i];moved.Add(cell);
            }
            return moved;
        }

        public static readonly Vector2Int InvalidGoal = new Vector2Int(-1, -1);

        public static int Shuffle(int columns, BoardCell[] floor, BlockColorData[] colors, BlockEdgeData[] edges,
            int[] groups, BlockColorData[] ropes, Vector2Int[] goalPositions, int steps, ISet<int> lockedColors = null)
        {
            int moved = 0;var occupied = new List<int>();
            for (int attempt = 0; attempt < steps * 30 && moved < steps; attempt++)
            {
                occupied.Clear();for (int i = 0; i < colors.Length; i++)if(colors[i] != null)occupied.Add(i);
                if (occupied.Count == 0) break;
                int index = occupied[Random.Range(0, occupied.Count)];
                if (colors[index].IsObstacle || ropes[index] != null || lockedColors != null && lockedColors.Contains(colors[index].ColorId)) continue;
                List<int> piece = BlockShapeLayout.Members(index, groups, colors);
                Vector2Int delta = BlockShapeLayout.Directions[Random.Range(0, 4)];
                if (!CanPlace(columns, floor, colors, piece, delta)) continue;
                Move(columns, colors, edges, groups, ropes, goalPositions, piece, delta);moved++;
            }
            return moved;
        }
    }
}
