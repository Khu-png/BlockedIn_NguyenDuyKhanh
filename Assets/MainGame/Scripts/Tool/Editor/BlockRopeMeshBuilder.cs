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
            var key = new StringBuilder(); foreach (Vector2Int cell in cells) key.Append(cell.x).Append(':').Append(cell.y).Append(';');
            string path = RopeAssetBuilder.Folder + "/RopeShape_" + Hash128.Compute(key.ToString()) + "_" + half + ".asset";
            Mesh result = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (result != null) return result;
            Mesh source = RopeAssetBuilder.Mesh("rope/Rope_Segment");
            var occupied = new HashSet<Vector2Int>(cells);
            var pieces = new List<CombineInstance>();
            float center = 0f; foreach (Vector2Int cell in cells) center += cell.x; center /= cells.Length;
            foreach (Vector2Int cell in cells)
            {
                Vector3 p = new Vector3(cell.x, .52f, -cell.y);
                if (!occupied.Contains(cell + Vector2Int.down)) Add(p + new Vector3(-.5f, 0, .5f), Vector3.right, source, center, half, pieces);
                if (!occupied.Contains(cell + Vector2Int.right)) Add(p + new Vector3(.5f, 0, .5f), Vector3.back, source, center, half, pieces);
                if (!occupied.Contains(cell + Vector2Int.up)) Add(p + new Vector3(.5f, 0, -.5f), Vector3.left, source, center, half, pieces);
                if (!occupied.Contains(cell + Vector2Int.left)) Add(p + new Vector3(-.5f, 0, -.5f), Vector3.forward, source, center, half, pieces);
            }
            result = new Mesh { name = "RopeShape_" + half };
            result.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            result.CombineMeshes(pieces.ToArray(), true, true);
            result.RecalculateBounds(); AssetDatabase.CreateAsset(result, path);
            return result;
        }
        static void Add(Vector3 start, Vector3 direction, Mesh mesh, float center, int half, List<CombineInstance> pieces)
        {
            const int count = 7;
            for (int i = 0; i < count; i++)
            {
                Vector3 midpoint = start + direction * ((i + .5f) / count);
                if ((midpoint.x < center ? 0 : 1) != half) continue;
                Vector3 scale = new Vector3(.65f, .65f, 1f / count / Mathf.Max(.001f, mesh.bounds.size.z));
                Matrix4x4 matrix = Matrix4x4.TRS(midpoint, Quaternion.LookRotation(direction, Vector3.up), scale)
                    * Matrix4x4.Translate(-mesh.bounds.center);
                pieces.Add(new CombineInstance { mesh = mesh, transform = matrix });
            }
        }
    }
}
