using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public static class BlockPlacementMesh
    {
        const float Radius = .12f;
        const float BorderWidth = .025f;
        static readonly Vector2Int[] corners =
        {
            Vector2Int.one, new Vector2Int(-1, 1), -Vector2Int.one, new Vector2Int(1, -1)
        };

        public static Mesh Build(Vector2Int[] cells)
        {
            var occupied = new HashSet<Vector2Int>(cells);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            foreach (Vector2Int cell in cells) AddCell(cell);
            var mesh = new Mesh { name = "BlockPlacementFootprint" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;

            int Add(Vector2 point, float alpha)
            {
                int index = vertices.Count;
                vertices.Add(new Vector3(point.x, 0, point.y));
                colors.Add(new Color(1, 1, 1, alpha));
                return index;
            }

            void Triangle(int a, int b, int c) => triangles.AddRange(new[] { a, b, c });

            void AddCell(Vector2Int cell)
            {
                Vector2 center = new Vector2(cell.x, -cell.y);
                List<Vector2> outline = Outline(cell, occupied);
                int middle = Add(center, .22f);
                int first = vertices.Count;
                foreach (Vector2 point in outline) Add(point, .22f);
                for (int i = 0; i < outline.Count; i++)
                {
                    int next = (i + 1) % outline.Count;
                    Triangle(middle, first + next, first + i);
                    Vector2 a = outline[i], b = outline[next];
                    Vector2 relative = (a + b) * .5f - center;
                    Vector2Int neighbor = cell;
                    if (Mathf.Abs(relative.x) > Mathf.Abs(relative.y)) neighbor.x += relative.x > 0 ? 1 : -1;
                    else neighbor.y += relative.y > 0 ? -1 : 1;
                    if (occupied.Contains(neighbor) || (b - a).sqrMagnitude < .000001f) continue;
                    int edge = Add(a, .85f);
                    Add(b, .85f);
                    Add(Inset(b, cell, occupied), .85f);
                    Add(Inset(a, cell, occupied), .85f);
                    Triangle(edge, edge + 1, edge + 2);
                    Triangle(edge, edge + 2, edge + 3);
                }
            }
        }

        static Vector2 Inset(Vector2 point, Vector2Int cell, HashSet<Vector2Int> occupied)
        {
            Vector2 center = new Vector2(cell.x, -cell.y);
            Vector2 relative = point - center;
            int x = relative.x > 0 ? 1 : -1;
            int y = relative.y > 0 ? 1 : -1;
            bool outsideX = !occupied.Contains(cell + new Vector2Int(x, 0));
            bool outsideY = !occupied.Contains(cell + new Vector2Int(0, -y));
            if (outsideX && outsideY)
            {
                Vector2 arcCenter = center + new Vector2(x, y) * (.5f - Radius);
                return point - (point - arcCenter).normalized * BorderWidth;
            }
            if (outsideX) point.x -= x * BorderWidth;
            if (outsideY) point.y -= y * BorderWidth;
            return point;
        }

        static List<Vector2> Outline(Vector2Int cell, HashSet<Vector2Int> occupied)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2Int sign = corners[i];
                bool rounded = !occupied.Contains(cell + new Vector2Int(sign.x, 0))
                    && !occupied.Contains(cell + new Vector2Int(0, sign.y));
                float radius = rounded ? Radius : 0;
                Vector2 center = new Vector2(cell.x + sign.x * (.5f - radius),
                    -cell.y - sign.y * (.5f - radius));
                for (int step = 0; step <= (rounded ? 4 : 0); step++)
                {
                    float angle = (i * 90 + step * 22.5f) * Mathf.Deg2Rad;
                    points.Add(center + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * radius);
                }
            }
            return points;
        }
    }
}
