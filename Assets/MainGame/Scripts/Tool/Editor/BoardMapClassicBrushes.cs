using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        void DrawClassicBrushes()
        {
            brushIndex = GUILayout.Toolbar(brushIndex, new[] { "Floor", "Wall", "Block" });
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Floor")) Fill(BoardCell.Floor);
                if (GUILayout.Button("Example 6×6")) Example();
            }
        }
    }
}
