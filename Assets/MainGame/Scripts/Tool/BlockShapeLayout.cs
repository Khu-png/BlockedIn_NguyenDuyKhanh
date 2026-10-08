using System;
using System.Collections.Generic;
using BlockedIn.Blocks;
using UnityEngine;

namespace BlockedIn.MapTools
{
    public static class BlockShapeLayout
    {
        public static readonly Vector2Int[] Directions = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };

        public static int Neighbor(int index, int side, int columns, int count)
        {
            Vector2Int position = new Vector2Int(index % columns, index / columns) + Directions[side];
            if (position.x < 0 || position.x >= columns || position.y < 0 || position.y >= count / columns) return -1;
            return position.y * columns + position.x;
        }

        public static List<int> Members(int index, int[] groups, BlockColorData[] colors)
        {
            var result = new List<int>();
            if (colors[index] == null) return result;
            int id = groups != null && groups.Length == colors.Length ? groups[index] : 0;
            if (id <= 0) { result.Add(index); return result; }
            for (int i = 0; i < colors.Length; i++)
                if (colors[i] != null && groups[i] == id) result.Add(i);
            return result;
        }

        public static bool Connected(ICollection<int> members, int columns, int count)
        {
            if (members.Count == 0) return false;
            var remaining = new HashSet<int>(members);
            var queue = new Queue<int>();
            foreach (int index in members) { queue.Enqueue(index); remaining.Remove(index); break; }
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                for (int side = 0; side < 4; side++)
                {
                    int next = Neighbor(index, side, columns, count);
                    if (remaining.Remove(next)) queue.Enqueue(next);
                }
            }
            return remaining.Count == 0;
        }

        public static void Validate(int columns, BlockColorData[] colors, int[] groups)
        {
            if (groups == null || groups.Length == 0) return;
            if (colors == null || groups.Length != colors.Length)
                throw new ArgumentException("Shape groups must match the block grid.");
            var checkedGroups = new HashSet<int>();
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] < 0 || (groups[i] > 0 && colors[i] == null))
                    throw new ArgumentException("Shape groups must contain block cells only.");
                if (groups[i] == 0 || !checkedGroups.Add(groups[i])) continue;
                List<int> members = Members(i, groups, colors);
                if (!Connected(members, columns, colors.Length))
                    throw new ArgumentException("Each merged block must be connected by shared edges.");
                foreach (int cell in members)
                    if (colors[cell].ColorId != colors[i].ColorId)
                        throw new ArgumentException("All cells in a merged block must share a color ID.");
            }
        }
    }
}
