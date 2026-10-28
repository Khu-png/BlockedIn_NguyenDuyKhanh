using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    static class MatchSweepShape
    {
        static readonly Vector2Int[] offsets = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };

        public static bool TryJoin(Block first, Block second, out Vector4 line)
        {
            line = Vector4.zero;
            int joinedSide = -1;
            Vector2 start = default, end = default, tangent = default;
            foreach (Vector2Int cell in first.Cells)
                for (int side = 0; side < 4; side++)
                {
                    if (joinedSide >= 0 && side != joinedSide) continue;
                    Vector2Int other = first.GridPosition + cell + offsets[side] - second.GridPosition;
                    if (!second.ContainsCell(other)) continue;
                    EdgeType a = first.EdgeAt(cell, (BlockSide)side);
                    EdgeType b = second.EdgeAt(other, (BlockSide)((side + 2) % 4));
                    if (!(a == EdgeType.Tab && b == EdgeType.Socket || a == EdgeType.Socket && b == EdgeType.Tab)) continue;
                    Vector2 center = first.GridPosition + cell + (Vector2)offsets[side] * .5f;
                    center.y = -center.y;
                    if (joinedSide < 0)
                    {
                        joinedSide = side;
                        tangent = side % 2 == 0 ? Vector2.right : Vector2.up;
                        start = center - tangent * .5f;
                        end = center + tangent * .5f;
                    }
                    if (Vector2.Dot(center - tangent * .5f, tangent) < Vector2.Dot(start, tangent)) start = center - tangent * .5f;
                    if (Vector2.Dot(center + tangent * .5f, tangent) > Vector2.Dot(end, tangent)) end = center + tangent * .5f;
                }
            line = new Vector4(start.x, start.y, end.x, end.y);
            return joinedSide >= 0;
        }

        public static Vector2Int[] Cells(Block first, Block second)
            => Cells(new[] { first, second });

        public static List<Block> Connected(Block first, Block second, IReadOnlyList<Block> blocks)
        {
            var connected = new List<Block> { first, second };
            if (blocks == null) return connected;
            for (int index = 0; index < connected.Count; index++)
                foreach (Block candidate in blocks)
                {
                    if (candidate == null || connected.Contains(candidate) || !candidate.isActiveAndEnabled
                        || candidate.IsDragging || candidate.IsSettling || candidate.IsFrozen
                        || candidate.IsRoped || candidate.IsRopeCutting) continue;
                    if (BlockConnectionRules.CanMatch(connected[index], candidate)
                        && TryJoin(connected[index], candidate, out _)) connected.Add(candidate);
                }
            return connected;
        }

        public static Vector2Int[] Cells(IReadOnlyList<Block> blocks)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (Block block in blocks)
                foreach (Vector2Int cell in block.Cells) cells.Add(block.GridPosition + cell);
            var result = new Vector2Int[cells.Count];
            cells.CopyTo(result);
            return result;
        }

        public static float Radius(Vector2Int[] cells, Vector4 line)
        {
            Vector2 a = new Vector2(line.x, line.y), b = new Vector2(line.z, line.w);
            Vector2 segment = b - a;
            float radius = .1f;
            foreach (Vector2Int cell in cells)
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    {
                        Vector2 point = new Vector2(cell.x + x * .5f, -cell.y + y * .5f);
                        Vector2 nearest = a + segment * Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                        radius = Mathf.Max(radius, Vector2.Distance(point, nearest));
                    }
            return radius;
        }
    }
}
