using BlockedIn.Blocks;
using BlockedIn.Blocks.Editor;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed partial class BoardMapWindow
    {
        void EnsureBlocks()
        {
            if (blockColors == null || blockColors.Length != columns * rows)
                blockColors = new BlockColorData[columns * rows];
            if (blockEdges == null || blockEdges.Length != columns * rows)
                blockEdges = new BlockEdgeData[columns * rows];
            EnsureGroups();
            for (int i = 0; i < blockEdges.Length; i++) blockEdges[i].Normalize();
        }

        void DrawGrid()
        {
            EnsureBlocks();
            const float size = 30;
            Rect grid = GUILayoutUtility.GetRect(columns * size, rows * size, GUILayout.ExpandWidth(false));
            PaintGrid(grid, size);
            var groupLabels = GroupLabels();
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    Rect rect = CellRect(grid, size, x, y);
                    Color tint = blockColors[index] != null ? BlockColorPalette.Tint(blockColors[index])
                        : cells[index] == MapTools.BoardCell.Wall ? new Color(.85f, .68f, .28f)
                        : new Color(.20f, .52f, .65f);
                    EditorGUI.DrawRect(rect, tint);
                    if (editShape && selectedCells.Contains(index))
                    {
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), Color.white);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), Color.white);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), Color.white);
                        EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), Color.white);
                    }
                    string label = blockColors[index] != null ? (blockGroups[index] > 0 ? "G" + groupLabels[blockGroups[index]] : "B") : cells[index] == MapTools.BoardCell.Wall ? "W" : "·";
                    Color oldColor = GUI.contentColor;
                    GUI.contentColor = tint.grayscale > .5f ? Color.black : Color.white;
                    GUI.Label(rect, label, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { normal = { textColor = GUI.contentColor } });
                    GUI.contentColor = oldColor;
                }
        }

        static Rect CellRect(Rect grid, float size, int x, int y)
        {
            return new Rect(grid.x + x * size, grid.y + y * size, size - 2, size - 2);
        }

        void PaintGrid(Rect grid, float size)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && grid.Contains(e.mousePosition))
            {
                int x = Mathf.FloorToInt((e.mousePosition.x - grid.x) / size);
                int y = Mathf.FloorToInt((e.mousePosition.y - grid.y) / size);
                int index = y * columns + x;
                if (editShape) { SelectShapeCell(index); e.Use(); return; }
                if (blockColors[index] != null)
                {
                    OpenPalette(CellRect(grid, size, x, y), index);
                    e.Use();
                    return;
                }
                painting = true;
                Undo.IncrementCurrentGroup();
                paintUndoGroup = Undo.GetCurrentGroup();
            }
            if (painting && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && grid.Contains(e.mousePosition))
            {
                int x = Mathf.FloorToInt((e.mousePosition.x - grid.x) / size);
                int y = Mathf.FloorToInt((e.mousePosition.y - grid.y) / size);
                Run(() => PaintCell(y * columns + x));
                e.Use();
                Repaint();
            }
            if (painting && (e.rawType == EventType.MouseUp || e.type == EventType.Ignore))
            {
                painting = false;
                Undo.CollapseUndoOperations(paintUndoGroup);
            }
        }

        void PaintCell(int index)
        {
            BlockColorData color = brushIndex == 2 ? BlockAssetBuilder.LoadColor("Blue") : null;
            if (brushIndex == 2 && color == null)
            {
                BlockAssetBuilder.CreateColors();
                color = BlockAssetBuilder.LoadColor("Blue");
            }
            Undo.RecordObject(this, "Paint map");
            if (blockColors[index] == null || color == null) blockEdges[index] = default;
            cells[index] = brushIndex == 1 ? MapTools.BoardCell.Wall : MapTools.BoardCell.Floor;
            blockColors[index] = color;
            blockGroups[index] = 0;
            RepairGroups();
        }

        void OpenPalette(Rect rect, int index)
        {
            PopupWindow.Show(rect, new BlockColorPalette(blockColors[index], blockEdges[index],
                color => SetBlockColor(index, color), edges => SetBlockEdges(index, edges), InternalEdges(index)));
        }

        void SetBlockColor(int index, BlockColorData color)
        {
            if (index >= blockColors.Length) return;
            Undo.RecordObject(this, "Change block color");
            foreach (int cell in BlockShapeLayout.Members(index, blockGroups, blockColors))
            {
                blockColors[cell] = color;
                if (color == null) { blockEdges[cell] = default; blockGroups[cell] = 0; }
            }
            Repaint();
        }

        void SetBlockEdges(int index, BlockEdgeData edges)
        {
            if (index < 0 || index >= blockEdges.Length || blockColors[index] == null) return;
            Undo.RecordObject(this, "Change block edges");
            bool[] internalEdges = InternalEdges(index);
            for (int side = 0; side < 4; side++)
                if (internalEdges[side]) edges.Set((BlockSide)side, EdgeType.Default);
            blockEdges[index] = edges;
            Repaint();
        }
    }
}
