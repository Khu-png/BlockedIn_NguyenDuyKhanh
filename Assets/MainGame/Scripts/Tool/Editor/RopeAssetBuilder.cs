using System;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class RopeAssetBuilder
    {
        public const string Folder = "Assets/MainGame/Prefabs/Rope";
        const string Resources = "Assets/MainGame/Resrouces/BlockedIn/BlockedIn/";
        [Serializable] sealed class CutData { public Rotation[] m_RotationCurves; }
        [Serializable] sealed class Rotation { public string path; public Curve curve; }
        [Serializable] sealed class Curve { public RotationKey[] m_Curve; }
        [Serializable] sealed class RotationKey
        { public float time; public Quaternion value, inSlope, outSlope; }

        public static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/MainGame/Prefabs", "Rope");
        }
        public static Mesh Mesh(string path)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(Resources + "models/obj/" + path + ".obj"))
                if (asset is Mesh mesh) return mesh;
            throw new InvalidOperationException("Missing rope model: " + path);
        }
        public static Material Material(string name, string texture, Shader shader)
        {
            EnsureFolder();
            string path = Folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Texture image = AssetDatabase.LoadAssetAtPath<Texture>(Resources + "models/" + texture + ".png");
            if (image == null) throw new InvalidOperationException("Missing rope texture: " + texture);
            material = new Material(shader) { name = name };
            material.SetTexture("_BaseMap", image); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .25f); material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        public static AnimationClip CutClip()
        {
            EnsureFolder(); string path = Folder + "/ScissorsCut.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(Resources + "data/animations/Cut.json");
            if (json == null) throw new InvalidOperationException("Missing scissors Cut animation data.");
            CutData data = JsonUtility.FromJson<CutData>(json.text);
            clip = new AnimationClip { name = "ScissorsCut", legacy = true, frameRate = 60f, wrapMode = WrapMode.ClampForever };
            foreach (Rotation rotation in data.m_RotationCurves)
            {
                if (rotation.path != "Pivot/Pivot_01" && rotation.path != "Pivot/Pivot_02") continue;
                var y = new AnimationCurve(); var w = new AnimationCurve();
                foreach (RotationKey key in rotation.curve.m_Curve)
                {
                    y.AddKey(new Keyframe(key.time, -key.value.z, -key.inSlope.z, -key.outSlope.z));
                    w.AddKey(new Keyframe(key.time, key.value.w, key.inSlope.w, key.outSlope.w));
                }
                clip.SetCurve(rotation.path, typeof(Transform), "localRotation.x", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve(rotation.path, typeof(Transform), "localRotation.y", y);
                clip.SetCurve(rotation.path, typeof(Transform), "localRotation.z", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve(rotation.path, typeof(Transform), "localRotation.w", w);
            }
            clip.EnsureQuaternionContinuity(); AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
        public static Transform Scissors(Transform parent, Shader shader)
        {
            var root = new GameObject("Scissors"); root.transform.SetParent(parent, false);
            var pivot = new GameObject("Pivot"); pivot.transform.SetParent(root.transform, false);
            var materials = new[] { Material("ScissorsHandle", "obj/textures/Scissors_Handle", shader),
                Material("ScissorsBlade", "obj/textures/Scissors_Base_color", shader) };
            for (int i = 1; i <= 2; i++)
            {
                var arm = new GameObject("Pivot_0" + i); arm.transform.SetParent(pivot.transform, false);
                arm.transform.localScale = Vector3.one * .6f;
                AddScissorParts(arm.transform, i, materials);
            }
            return root.transform;
        }
        static void AddScissorParts(Transform arm, int index, Material[] materials)
        {
            string path = Resources + "models/obj/powerup_tools/Scissor_0" + index + ".obj";
            int count = 0;
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is not Mesh mesh) continue;
                int materialIndex = mesh.name.EndsWith("_sub0") ? 0 : mesh.name.EndsWith("_sub1") ? 1 : -1;
                if (materialIndex < 0) throw new InvalidOperationException("Unknown scissors mesh part: " + mesh.name);
                var part = new GameObject(materialIndex == 0 ? "Handle" : "Blade");
                part.transform.SetParent(arm, false);
                MeshFilter filter = part.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
                MeshRenderer renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials[materialIndex];
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                count++;
            }
            if (count != 2) throw new InvalidOperationException("Scissors need both a handle mesh and a blade mesh.");
        }
    }
}
