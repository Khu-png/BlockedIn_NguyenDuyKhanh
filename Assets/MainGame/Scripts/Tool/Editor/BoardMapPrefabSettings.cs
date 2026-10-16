using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        [SerializeField] bool showPrefabSettings;

        void DrawPrefabSettings()
        {
            showPrefabSettings = EditorGUILayout.Foldout(showPrefabSettings, "Board prefab settings", true);
            if (!showPrefabSettings) return;
            tilePrefab = PrefabField("Tile prefab", tilePrefab);
            backdropPrefab = PrefabField("Backdrop prefab", backdropPrefab);
            wallPrefab = PrefabField("Wall prefab", wallPrefab);
        }

    }
}
