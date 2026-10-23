using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockIceContour
    {
        public static Vector2[] Outline(Vector2 center, float[] reach, float radius, int exposed, int tabs, bool outer)
        {
            var points = new List<Vector2>();
            int[] sides = { 1, 2, 3, 0 };
            for (int corner = 0; corner < 4; corner++)
            {
                int before = corner, after = sides[corner];
                float cut = (exposed & (1 << before)) != 0 && (exposed & (1 << after)) != 0 ? radius : 0;
                Vector2 pivot = new Vector2(corner < 2 ? reach[1] - cut : -reach[3] + cut,
                    corner == 0 || corner == 3 ? reach[0] - cut : -reach[2] + cut);
                for (int step = 0; step <= 4; step++)
                {
                    float angle = (90 - corner * 90 - step * 22.5f) * Mathf.Deg2Rad;
                    points.Add(center + pivot + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cut);
                }
                int side = sides[corner];
                if ((tabs & exposed & (1 << side)) != 0) AddTab(points, center, side, reach[side], outer);
            }
            return points.ToArray();
        }

        static void AddTab(List<Vector2> points, Vector2 center, int side, float start, bool outer)
        {
            Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
            Vector2 outward = directions[side], tangent = new Vector2(outward.y, -outward.x);
            float width = outer ? .18f : .115f, tip = outer ? .625f : .61f, radius = outer ? .065f : .035f;
            Add(-width, start);
            Add(-width, tip - radius);
            for (int i = 1; i <= 3; i++)
            {
                float angle = i * Mathf.PI / 6;
                Add(-width + radius - Mathf.Cos(angle) * radius, tip - radius + Mathf.Sin(angle) * radius);
            }
            Add(width - radius, tip);
            for (int i = 1; i <= 3; i++)
            {
                float angle = i * Mathf.PI / 6;
                Add(width - radius + Mathf.Sin(angle) * radius, tip - radius + Mathf.Cos(angle) * radius);
            }
            Add(width, start);
            void Add(float across, float along) => points.Add(center + tangent * across + outward * along);
        }

        public static List<int> Triangulate(Vector2[] points)
        {
            var remaining = new List<int>();
            var result = new List<int>();
            for (int i = 0; i < points.Length; i++)
                if ((points[i] - points[(i + 1) % points.Length]).sqrMagnitude > .000001f) remaining.Add(i);
            int limit = points.Length * points.Length;
            while (remaining.Count > 2 && limit-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (Cross(points[b] - points[a], points[c] - points[b]) >= -.000001f) continue;
                    bool contains = false;
                    foreach (int p in remaining)
                    {
                        if (p == a || p == b || p == c) continue;
                        if (Cross(points[b] - points[a], points[p] - points[a]) < -.000001f
                            && Cross(points[c] - points[b], points[p] - points[b]) < -.000001f
                            && Cross(points[a] - points[c], points[p] - points[c]) < -.000001f)
                        { contains = true; break; }
                    }
                    if (contains) continue;
                    result.Add(a); result.Add(b); result.Add(c);
                    remaining.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) break;
            }
            return result;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
