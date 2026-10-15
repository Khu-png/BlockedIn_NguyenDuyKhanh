using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public static class BlockSplitLayout
    {
        static readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static bool TryDivide(Vector2Int[] cells, out List<int> first, out List<int> second)
        {
            first = new List<int>();
            second = new List<int>();
            if (cells == null || cells.Length < 2) return false;
            var parents = new int[cells.Length];
            for (int i = 0; i < parents.Length; i++) parents[i] = -2;
            parents[0] = -1;
            var queue = new Queue<int>();
            var order = new List<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                order.Add(current);
                foreach (Vector2Int direction in directions)
                {
                    int next = System.Array.IndexOf(cells, cells[current] + direction);
                    if (next < 0 || parents[next] != -2) continue;
                    parents[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (order.Count != cells.Length) return false;
            var sizes = new int[cells.Length];
            for (int i = order.Count - 1; i >= 0; i--)
            {
                int index = order[i];
                sizes[index]++;
                if (parents[index] >= 0) sizes[parents[index]] += sizes[index];
            }
            int cut = 1;
            for (int i = 2; i < cells.Length; i++)
                if (Mathf.Abs(cells.Length - sizes[i] * 2) < Mathf.Abs(cells.Length - sizes[cut] * 2)) cut = i;
            for (int i = 0; i < cells.Length; i++)
            {
                int ancestor = i;
                while (ancestor >= 0 && ancestor != cut) ancestor = parents[ancestor];
                if (ancestor == cut) first.Add(i);
                else second.Add(i);
            }
            return first.Count > 0 && second.Count > 0;
        }
    }
}
