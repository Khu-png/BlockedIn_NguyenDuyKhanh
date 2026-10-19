using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockIcePerimeter
    {
        struct Edge
        {
            public Vector3 start, end, outward;
            public float length;
        }
        readonly Edge[] edges;
        public float Length { get; }
        public int EdgeCount => edges.Length;

        public BlockIcePerimeter(Mesh mesh)
        {
            var found = new List<Edge>();
            var vertices = mesh.vertices;
            var layers = new List<Vector2>();mesh.GetUVs(3, layers);
            float faceHeight = mesh.bounds.max.y;
            if (layers.Count == vertices.Length)
            {
                faceHeight = float.NegativeInfinity;
                for (int i = 0; i < vertices.Length; i++)
                    if (layers[i].x < .5f) faceHeight = Mathf.Max(faceHeight, vertices[i].y);
                if (float.IsNegativeInfinity(faceHeight)) faceHeight = mesh.bounds.max.y;
                for (int i = 0; i + 3 < vertices.Length; i++)
                {
                    if (Mathf.Abs(layers[i].x - 2) > .01f || layers[i].y > .01f
                        || Mathf.Abs(layers[i+1].x - 2) > .01f || layers[i+1].y > .01f) continue;
                    Vector3 start = vertices[i], end = vertices[i+1];
                    start.y = end.y = faceHeight;
                    Add(start, end);i += 3;
                }
            }
            if (found.Count == 0)
            {
                Bounds b = mesh.bounds;
                Vector3 a = new Vector3(b.min.x, faceHeight, b.max.z);
                Vector3 c = new Vector3(b.max.x, faceHeight, b.min.z);
                Add(a, new Vector3(c.x, faceHeight, a.z));
                Add(new Vector3(c.x, faceHeight, a.z), c);
                Add(c, new Vector3(a.x, faceHeight, c.z));
                Add(new Vector3(a.x, faceHeight, c.z), a);
            }
            edges = found.ToArray();
            foreach (Edge edge in edges) Length += edge.length;

            void Add(Vector3 start, Vector3 end)
            {
                Vector3 tangent = end - start;float length = tangent.magnitude;
                if (length < .0001f) return;
                Vector3 outward = new Vector3(-tangent.z, 0, tangent.x) / length;
                found.Add(new Edge { start = start, end = end, outward = outward, length = length });
            }
        }

        public void Sample(float distance, out Vector3 point, out Vector3 outward)
        {
            distance = Mathf.Repeat(distance, Length);
            foreach (Edge edge in edges)
            {
                if (distance <= edge.length)
                {
                    outward = edge.outward;
                    point = Vector3.Lerp(edge.start, edge.end, distance / edge.length)
                        + outward * .025f + Vector3.up * .015f;
                    return;
                }
                distance -= edge.length;
            }
            point = edges[0].start;outward = edges[0].outward;
        }
    }
}
