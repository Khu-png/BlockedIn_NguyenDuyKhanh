using System;
using System.Collections.Generic;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
namespace BlockedIn.EditorTools
{
    public static class PuzzleShapeCutter
    {
        public static void JoinColors(int columns, BlockColorData[] colors, BlockEdgeData[] edges, int[] groups, bool resetPorts = false)
        {
            Array.Clear(groups, 0, groups.Length);
            var visited = new HashSet<int>();
            int id = 1;
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null || !visited.Add(i)) continue;
                var members = new List<int>();
                var pending = new Queue<int>();pending.Enqueue(i);
                while (pending.Count > 0)
                {
                    int cell = pending.Dequeue();members.Add(cell);
                    for (int side = 0; side < 4; side++)
                    {
                        int next = BlockShapeLayout.Neighbor(cell, side, columns, colors.Length);
                        if (next >= 0 && colors[next] != null && colors[next].ColorId == colors[i].ColorId && visited.Add(next)) pending.Enqueue(next);
                    }
                }
                foreach (int cell in members)
                {
                    groups[cell] = members.Count > 1 ? id : 0;
                    if (resetPorts) edges[cell] = default;
                    for (int side = 0; side < 4; side++)
                    {
                        int next = BlockShapeLayout.Neighbor(cell, side, columns, colors.Length);
                        if (members.Contains(next)) edges[cell].Set((BlockSide)side, EdgeType.Default);
                    }
                }
                id++;
            }
        }
        public static void Prepare(int columns, BlockColorData[] colors, BlockEdgeData[] edges, int[] groups)
        {
            BlockShapeLayout.Validate(columns, colors, groups);
        }
        public static void Cut(int columns, BlockColorData[] colors, BlockEdgeData[] edges, int[] groups, List<int> selected)
        {
            if (selected.Count == 0 || colors[selected[0]] == null) throw new InvalidOperationException("Select the cells to cut off.");
            if (colors[selected[0]].IsObstacle) throw new InvalidOperationException("Obstacle shapes cannot be cut into puzzle pieces.");
            List<int> whole = BlockShapeLayout.Members(selected[0], groups, colors);
            var chosen = new HashSet<int>(selected);
            foreach (int cell in chosen)
                if (!whole.Contains(cell)) throw new InvalidOperationException("Select cells from one piece only.");
            var remainder = whole.FindAll(cell => !chosen.Contains(cell));
            if (remainder.Count == 0) throw new InvalidOperationException("Select part of the shape, leaving at least one cell in the other piece.");
            if (!BlockShapeLayout.Connected(chosen, columns, colors.Length) || !BlockShapeLayout.Connected(remainder, columns, colors.Length))
                throw new InvalidOperationException("Both pieces must remain connected after the cut.");
            int newId = 1;foreach (int id in groups) newId = Math.Max(newId, id + 1);
            foreach (int cell in whole)
                for (int side = 0; side < 4; side++)
                {
                    int next = BlockShapeLayout.Neighbor(cell, side, columns, colors.Length);
                    if (whole.Contains(next) && chosen.Contains(cell) != chosen.Contains(next))
                        edges[cell].Set((BlockSide)side, side < 2 ? EdgeType.Tab : EdgeType.Socket);
                }
            int oldId = groups[whole[0]];
            foreach (int cell in chosen) groups[cell] = chosen.Count > 1 ? newId : 0;
            foreach (int cell in remainder) groups[cell] = remainder.Count > 1 ? oldId : 0;
        }
    }
}
