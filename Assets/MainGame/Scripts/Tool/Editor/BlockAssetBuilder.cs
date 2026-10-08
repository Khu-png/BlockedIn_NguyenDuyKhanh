using System;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.Blocks.Editor
{
    public static class BlockAssetBuilder
    {
        public const string ColorFolder = "Assets/MainGame/ScriptableObject/BlockColors";
        public const string PrefabPath = "Assets/MainGame/Prefabs/Block 1x1 Base.prefab";
        static readonly string[] colors = { "Black", "Blue", "ForestGreen", "Green", "Obstacle", "Orange",
            "Pink", "Purple", "Red", "Turquoise", "White", "Yellow" };
        public static void CreateColors()
        {
            if (!AssetDatabase.IsValidFolder(ColorFolder))
                AssetDatabase.CreateFolder("Assets/MainGame/ScriptableObject", "BlockColors");
            for (int i = 0; i < colors.Length; i++)
            {
                string path = ColorFolder + "/BlockColor_" + colors[i] + ".asset";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) continue;
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainGame/Material/Block_" + colors[i] + ".mat");
                if (material == null) throw new InvalidOperationException("Missing material for " + colors[i]);
                var color = ScriptableObject.CreateInstance<BlockColorData>();
                color.Configure(i, material);
                AssetDatabase.CreateAsset(color, path);
            }
            AssetDatabase.SaveAssets();
        }

        public static BlockColorData LoadColor(string name)
        {
            return AssetDatabase.LoadAssetAtPath<BlockColorData>(ColorFolder + "/BlockColor_" + name + ".asset");
        }

        public static Block LoadBasePrefab()
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(PrefabPath))
                if (asset is Block block) return block;
            throw new InvalidOperationException("The base prefab needs its serialized Block component.");
        }
    }
}
