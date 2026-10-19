using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class PlacementPreviewMeshCache : IDisposable
    {
        readonly Dictionary<Vector2Int[], Mesh> meshes = new Dictionary<Vector2Int[], Mesh>();

        public Mesh Get(Vector2Int[] footprint)
        {
            if (!meshes.TryGetValue(footprint, out Mesh mesh))
            {
                mesh = BlockPlacementMesh.Build(footprint);
                meshes.Add(footprint, mesh);
            }
            return mesh;
        }
        public void Dispose()
        {
            foreach (Mesh mesh in meshes.Values)
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
        }
    }
}
