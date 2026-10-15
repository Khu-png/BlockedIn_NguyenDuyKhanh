using System;
using BlockedIn.Blocks;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class RopeEffectsBuilder
    {
        static Material EffectMaterial(string name, string texture)
        {
            string path = RopeAssetBuilder.Folder + "/" + name + ".mat";
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/MainGame/Material/RopeEffect.shader");
            Texture image = AssetDatabase.LoadAssetAtPath<Texture>(
                "Assets/MainGame/Resrouces/BlockedIn/BlockedIn/images/game/vfx/" + texture + ".png");
            if (shader == null || image == null) throw new InvalidOperationException("Missing rope effect shader or texture.");
            result = new Material(shader) { name = name }; result.SetTexture("_BaseMap", image);
            AssetDatabase.CreateAsset(result, path); return result;
        }
        static Mesh Quad()
        {
            string path = RopeAssetBuilder.Folder + "/RopeEffectQuad.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "RopeEffectQuad" };
            mesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static MeshRenderer Billboard(string name, Transform parent, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            MeshFilter filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = Quad();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false; return renderer;
        }
        public static BlockRopeEffects Build(Transform root, Transform scissors)
        {
            var tail = new GameObject("ScissorsTrail"); tail.transform.SetParent(scissors, false);
            TrailRenderer trail = tail.AddComponent<TrailRenderer>();
            trail.sharedMaterial = EffectMaterial("ScissorsTrail", "Glow_Trail_01");
            trail.time = .18f; trail.minVertexDistance = .025f;
            trail.startWidth = .12f; trail.endWidth = 0f;
            trail.startColor = new Color(1f, .9f, .6f, .55f); trail.endColor = new Color(1f, .9f, .6f, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MeshRenderer glow = Billboard("ScissorsGlow", scissors, EffectMaterial("ScissorsGlow", "Glow_01"));
            glow.transform.localPosition = Vector3.up * .025f; glow.transform.localScale = Vector3.one * .9f;
            var stars = new Transform[6]; Material sparkle = EffectMaterial("ScissorsSparkle", "Sparkle_01");
            for (int i = 0; i < stars.Length; i++) stars[i] = Billboard("CutSpark_" + i, root, sparkle).transform;
            BlockRopeEffects effects = root.gameObject.AddComponent<BlockRopeEffects>();
            effects.Configure(trail, glow, stars); return effects;
        }
    }
}
