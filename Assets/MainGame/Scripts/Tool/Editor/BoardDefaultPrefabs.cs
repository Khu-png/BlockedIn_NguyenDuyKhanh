using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardDefaultPrefabs
    {
        const string Folder = "Assets/MainGame/Prefabs/Board/";

        public static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(Folder + name + ".prefab");
        }
    }
}
