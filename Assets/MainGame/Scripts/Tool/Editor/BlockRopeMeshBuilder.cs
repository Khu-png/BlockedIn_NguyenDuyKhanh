using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockRopeMeshBuilder
    {
        public static Mesh Load(Vector2Int[] cells, int half)
        {
            var key = new StringBuilder("DiagonalV3;"); foreach (Vector2Int cell in cells) key.Append(cell.x).Append(':').Append(cell.y).Append(';');
            string path = RopeAssetBuilder.Folder + "/RopeDiagonal_" + Hash128.Compute(key.ToString()) + "_" + half + ".asset";
            Mesh result = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (result != null) return result;
            Mesh source = RopeAssetBuilder.Mesh("rope/Rope_Segment");
            var pieces = new List<CombineInstance>();
            Vector3 pivot = CutPoint(cells);
            for (int diagonal = 0; diagonal < 2; diagonal++)
            {
                Endpoints(cells, diagonal, out Vector3 start, out Vector3 end);
                Vector3 direction = (end - start).normalized;
                foreach (Vector2 interval in Intervals(cells, start, end))
                {
                    Vector3 a = Vector3.Lerp(start, end, interval.x), b = Vector3.Lerp(start, end, interval.y);
                    int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / .19f));
                    float length = Vector3.Distance(a, b) / count;
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 midpoint = a + direction * ((i + .5f) * length);
                        if ((midpoint.x < pivot.x ? 0 : 1) != half) continue;
                        Vector3 scale = new Vector3(.42f, .42f, length * 1.025f / Mathf.Max(.001f, source.bounds.size.z));
                        Matrix4x4 matrix = Matrix4x4.TRS(midpoint - pivot, Quaternion.LookRotation(direction, Vector3.up), scale)
                            * Matrix4x4.Translate(-source.bounds.center);
                        pieces.Add(new CombineInstance { mesh = source, transform = matrix });
                    }
                }
            }
            result = new Mesh { name = "RopeDiagonal_" + half };
            result.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            result.CombineMeshes(pieces.ToArray(), true, true);
            result.RecalculateBounds(); AssetDatabase.CreateAsset(result, path);
            return result;
        }
        public static Vector3 CutPoint(Vector2Int[] cells)
        {
            Endpoints(cells, 0, out Vector3 a, out Vector3 b);
            float best = float.PositiveInfinity; Vector3 point = (a + b) * .5f;
            foreach (Vector2 interval in Intervals(cells, a, b))
            {
                float t = Mathf.Clamp(.5f, interval.x, interval.y);
                if (Mathf.Abs(t - .5f) >= best) continue;
                best = Mathf.Abs(t - .5f); point = Vector3.Lerp(a, b, t);
            }
            return point;
        }
        static void Endpoints(Vector2Int[] cells, int diagonal, out Vector3 a, out Vector3 b)
        {
            Vector2 min = cells[0], max = cells[0];
            foreach (Vector2Int cell in cells) { min = Vector2.Min(min, cell); max = Vector2.Max(max, cell); }
            float y = .58f + diagonal * .018f;
            a = new Vector3(min.x - .39f, y, diagonal == 0 ? -min.y + .39f : -max.y - .39f);
            b = new Vector3(max.x + .39f, y, diagonal == 0 ? -max.y - .39f : -min.y + .39f);
        }
        static List<Vector2> Intervals(Vector2Int[] cells, Vector3 start, Vector3 end)
        {
            var ranges = new List<Vector2>(); Vector3 delta = end - start;
            foreach (Vector2Int cell in cells)
            {
                float enter = 0f, exit = 1f;
                if (Clip(start.x, delta.x, cell.x - .49f, cell.x + .49f, ref enter, ref exit)
                    && Clip(start.z, delta.z, -cell.y - .49f, -cell.y + .49f, ref enter, ref exit))
                    ranges.Add(new Vector2(enter, exit));
            }
            ranges.Sort((a, b) => a.x.CompareTo(b.x));
            var merged = new List<Vector2>();
            foreach (Vector2 range in ranges)
            {
                if (merged.Count == 0 || range.x - merged[merged.Count - 1].y > .025f / Mathf.Max(.01f, delta.magnitude))
                    merged.Add(range);
                else
                { Vector2 previous = merged[merged.Count - 1]; previous.y = Mathf.Max(previous.y, range.y); merged[merged.Count - 1] = previous; }
            }
            return merged;
        }
        static bool Clip(float start, float delta, float min, float max, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < .00001f) return start >= min && start <= max;
            float a = (min - start) / delta, b = (max - start) / delta;
            enter = Mathf.Max(enter, Mathf.Min(a, b)); exit = Mathf.Min(exit, Mathf.Max(a, b));
            return exit - enter > .00001f;
        }
    }
}
