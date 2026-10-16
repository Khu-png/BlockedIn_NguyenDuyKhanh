using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;
namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        bool HandleAssemblyClick(Event e, Rect grid, float size)
        {
            if (!UsesAssemblyWorkflow || (assemblyStage == 0 && !selectAssemblyGroups)
                || e.type != EventType.MouseDown || !grid.Contains(e.mousePosition)) return false;
            int x = Mathf.FloorToInt((e.mousePosition.x - grid.x) / size);
            int y = Mathf.FloorToInt((e.mousePosition.y - grid.y) / size);
            int index = y * columns + x;
            if (e.button == 1)
            {
                if (blockColors[index] != null) OpenPalette(CellRect(grid, size, x, y), index);
            }
            else if (e.button == 0)
            {
                if (assemblyStage == 0) ToggleAssemblyGroup(index);
                else if (assemblyStage == 1) SelectShapeCell(index);
                else if (blockColors[index] != null)
                    selectedCells = BlockShapeLayout.Members(index, blockGroups, blockColors);
                else if (selectedCells.Count > 0)
                {
                    int first = selectedCells[0];
                    Vector2Int delta = new Vector2Int(x - first % columns, y - first / columns);
                    Run(() => MoveAssemblyPiece(delta));
                }
            }
            e.Use();Repaint();return true;
        }
        void DrawAssemblyBridge(Rect rect, int index, Color tint)
        {
            if (!UsesAssemblyWorkflow || blockColors[index] == null || blockGroups[index] <= 0) return;
            int right = BlockShapeLayout.Neighbor(index, 1, columns, cells.Length);
            int bottom = BlockShapeLayout.Neighbor(index, 2, columns, cells.Length);
            bool joinsRight = right >= 0 && blockGroups[right] == blockGroups[index];
            bool joinsBottom = bottom >= 0 && blockGroups[bottom] == blockGroups[index];
            if (joinsRight) EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, 2, rect.height), tint);
            if (joinsBottom) EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 2), tint);
            if (joinsRight && joinsBottom && bottom + 1 < cells.Length && blockGroups[bottom+1] == blockGroups[index])
                EditorGUI.DrawRect(new Rect(rect.xMax, rect.yMax, 2, 2), tint);
        }
        void DrawAssemblyPorts(Rect grid, float size)
        {
            if (!UsesAssemblyWorkflow || assemblyStage == 0) return;
            for (int i = 0; i < blockColors.Length; i++)
            {
                if (blockColors[i] == null) continue;
                Rect rect = CellRect(grid, size, i % columns, i / columns);
                for (int side = 0; side < 4; side++)
                {
                    EdgeType edge = blockEdges[i].Get((BlockSide)side);
                    if (edge != EdgeType.Tab && edge != EdgeType.Socket) continue;
                    Vector2 direction = side == 0 ? Vector2.down : side == 1 ? Vector2.right : side == 2 ? Vector2.up : Vector2.left;
                    Vector2 point = rect.center + direction * (size * .5f - 3);
                    EditorGUI.DrawRect(new Rect(point.x-2, point.y-2, 4, 4), edge == EdgeType.Tab ? new Color(1,.8f,.1f) : new Color(.03f,.08f,.15f));
                }
            }
        }
    }
}
