using System.IO;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockIceAssetUpgrade
    {
        [MenuItem("Tools/Blocked In/Refresh Ice Shells")]
        public static void RefreshSavedLevels()
        {
            const string folder = "Assets/MainGame/ScriptableObject/Levels";
            foreach (string path in Directory.GetFiles(folder, "*.asset"))
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null || level.RuntimeBoard == null || level.RuntimeBoard.blockGameplay == null) continue;
                bool changed = false;
                foreach (Block block in level.RuntimeBoard.blockGameplay.Blocks)
                {
                    if (block == null || !block.IsFrozen) continue;
                    var data = new SerializedObject(block);
                    var ice = data.FindProperty("icePresentation").objectReferenceValue as BlockIceVisual;
                    if (ice == null || ice.Shell == null) continue;
                    string meshPath = AssetDatabase.GetAssetPath(ice.Shell.sharedMesh);
                    if (!meshPath.StartsWith("Assets/MainGame/Prefabs/Ice/", System.StringComparison.Ordinal)) continue;
                    Mesh mesh = BoardIceBuilder.LoadMesh(block);
                    if (ice.Shell.sharedMesh == mesh) continue;
                    ice.Shell.sharedMesh = mesh;
                    EditorUtility.SetDirty(ice.Shell);
                    changed = true;
                }
                if (changed) PrefabUtility.SavePrefabAsset(level.RuntimeBoard.gameObject);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
