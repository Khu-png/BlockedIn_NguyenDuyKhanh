using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BlockedIn.Blocks;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockHitboxBuilder
    {
        const string Folder = "Assets/MainGame/Mesh/Hitboxes";

        public static void Bake(Block block)
        {
            if (block.HitMeshSources == null || block.HitMeshSources.Length == 0)
                throw new InvalidOperationException("Assign the block prefab's mesh sources before generating.");
            MeshCollider collider = EnsureCollider(block);
            var parts = new List<CombineInstance>();
            var key = new StringBuilder("BlockHitbox_v1");
            foreach (MeshFilter source in block.HitMeshSources)
            {
                if (source == null) throw new InvalidOperationException("A block hitbox mesh source is missing.");
                if (!source.gameObject.activeInHierarchy) continue;
                AddSource(source, collider.transform, parts, key);
            }
            if (parts.Count == 0) throw new InvalidOperationException("The block has no visible meshes for its hitbox.");
            EnsureFolder();
            string path = Folder + "/BlockHit_" + Hash128.Compute(key.ToString()) + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) mesh = CreateMesh(parts, path);
            Undo.RecordObject(collider, "Update block mesh hitbox");
            collider.sharedMesh = null;
            collider.convex = false;
            collider.isTrigger = false;
            collider.sharedMesh = mesh;
            EditorUtility.SetDirty(collider);
        }

        static MeshCollider EnsureCollider(Block block)
        {
            if (block.HitCollider is MeshCollider current) return current;
            Collider previous = block.HitCollider;
            GameObject target = previous != null ? previous.gameObject : block.gameObject;
            MeshCollider collider = Undo.AddComponent<MeshCollider>(target);
            if (previous != null) collider.sharedMaterial = previous.sharedMaterial;
            Undo.RecordObject(block, "Assign block mesh hitbox");
            block.SetHitCollider(collider);
            if (previous != null) Undo.DestroyObjectImmediate(previous);
            EditorUtility.SetDirty(block);
            return collider;
        }

        static void AddSource(MeshFilter source, Transform target, List<CombineInstance> parts, StringBuilder key)
        {
            Mesh mesh = source.sharedMesh;
            if (mesh == null) throw new InvalidOperationException("A block hitbox source has no mesh.");
            Matrix4x4 matrix = target.worldToLocalMatrix * source.transform.localToWorldMatrix;
            // Round away transform noise so equal shapes share one persisted hitbox asset.
            for (int i = 0; i < 16; i++) matrix[i] = Mathf.Round(matrix[i] * 100000) / 100000;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
            string path = AssetDatabase.GetAssetPath(mesh);
            key.Append(guid).Append(':').Append(id).Append(':').Append(AssetDatabase.GetAssetDependencyHash(path));
            for (int i = 0; i < 16; i++) key.Append(':').Append(matrix[i].ToString("F5", CultureInfo.InvariantCulture));
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = subMesh, transform = matrix });
        }

        static Mesh CreateMesh(List<CombineInstance> parts, string path)
        {
            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            if (mesh.vertexCount == 0)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                throw new InvalidOperationException("Could not combine block meshes. Enable Read/Write on the source models.");
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/MainGame/Mesh")) AssetDatabase.CreateFolder("Assets/MainGame", "Mesh");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/MainGame/Mesh", "Hitboxes");
        }
    }
}
