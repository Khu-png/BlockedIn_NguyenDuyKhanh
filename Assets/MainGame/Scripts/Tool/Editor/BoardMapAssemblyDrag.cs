using System;
using System.Collections.Generic;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        readonly List<int> draggedAssemblyPiece = new List<int>();
        int assemblyDragControl;
        Vector2Int assemblyDragAnchor;
        Vector2Int assemblyDragDelta;
        bool assemblyDragMoved;

        bool HandleAssemblyDrag(Event e, Rect grid, float size)
        {
            if (!UsesAssemblyWorkflow || assemblyStage != 2) return false;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (assemblyDragControl != 0)
            {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape || e.type == EventType.Ignore)
                {
                    CancelAssemblyDrag();e.Use();Repaint();return true;
                }
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    assemblyDragDelta = AssemblyGridCell(e.mousePosition, grid, size) - assemblyDragAnchor;
                    assemblyDragMoved = true;
                    e.Use();Repaint();return true;
                }
                if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    assemblyDragDelta = AssemblyGridCell(e.mousePosition, grid, size) - assemblyDragAnchor;
                    FinishAssemblyDrag();e.Use();Repaint();return true;
                }
                return e.isMouse;
            }
            if (e.type != EventType.MouseDown || e.button != 0 || !grid.Contains(e.mousePosition)) return false;
            Vector2Int cell = AssemblyGridCell(e.mousePosition, grid, size);
            int index = cell.y * columns + cell.x;
            if (blockColors[index] == null) return false;
            selectedCells = BlockShapeLayout.Members(index, blockGroups, blockColors);
            draggedAssemblyPiece.AddRange(selectedCells);
            assemblyDragAnchor = cell;
            assemblyDragDelta = Vector2Int.zero;
            assemblyDragMoved = false;
            assemblyDragControl = control;
            GUIUtility.hotControl = control;
            GUIUtility.keyboardControl = 0;
            e.Use();Repaint();return true;
        }

        static Vector2Int AssemblyGridCell(Vector2 mouse, Rect grid, float size)
        {
            return new Vector2Int(Mathf.FloorToInt((mouse.x - grid.x) / size), Mathf.FloorToInt((mouse.y - grid.y) / size));
        }

        void FinishAssemblyDrag()
        {
            if (assemblyDragMoved && assemblyDragDelta != Vector2Int.zero)
            {
                if (CanPlaceOrSwap(draggedAssemblyPiece, assemblyDragDelta, out _))
                {
                    var piece = new List<int>(draggedAssemblyPiece);
                    Vector2Int delta = assemblyDragDelta;
                    Run(() => PlaceAssemblyPiece(piece, delta));
                }
                else assemblyStatus = "Drop cancelled: both groups must fit without overlapping walls, other groups, or the board edge.";
            }
            CancelAssemblyDrag();
        }

        void PlaceAssemblyPiece(List<int> piece, Vector2Int delta)
        {
            if (delta == Vector2Int.zero) return;
            if (!CanPlaceOrSwap(piece, delta, out List<int> other))
                throw new InvalidOperationException("The destination must fit both groups without overlapping walls, other groups, or the board edge.");
            Undo.RecordObject(this, other == null ? "Place puzzle piece" : "Swap puzzle groups");
            selectedCells = other == null
                ? PuzzlePieceMover.Move(columns, blockColors, blockEdges, blockGroups, ropeColors, piece, delta)
                : PuzzlePieceSwap.Apply(columns, blockColors, blockEdges, blockGroups, ropeColors, piece, other, delta);
            assemblyStatus = other == null ? null : "Groups swapped.";
        }

        bool CanPlaceOrSwap(List<int> piece, Vector2Int delta, out List<int> other)
        {
            other = null;
            return PuzzlePieceMover.CanPlace(columns, cells, blockColors, piece, delta)
                || PuzzlePieceSwap.TryPlan(columns, cells, blockColors, blockGroups, piece, delta, out other);
        }

        void CancelAssemblyDrag()
        {
            if (assemblyDragControl != 0 && GUIUtility.hotControl == assemblyDragControl) GUIUtility.hotControl = 0;
            assemblyDragControl = 0;
            assemblyDragMoved = false;
            draggedAssemblyPiece.Clear();
        }

        protected virtual void OnLostFocus() => CancelAssemblyDrag();

        void DrawAssemblyDragPreview(Rect grid, float size)
        {
            if (assemblyDragControl == 0 || !assemblyDragMoved) return;
            bool valid = CanPlaceOrSwap(draggedAssemblyPiece, assemblyDragDelta, out List<int> other);
            Color border = !valid ? new Color(1, .2f, .2f, 1) : other != null ? Color.cyan : new Color(.2f, 1, .4f, 1);
            foreach (int cell in draggedAssemblyPiece)
            {
                Rect source = CellRect(grid, size, cell % columns, cell / columns);
                EditorGUI.DrawRect(source, new Color(0, 0, 0, .45f));
            }
            DrawAssemblyGhost(draggedAssemblyPiece, assemblyDragDelta, grid, size, border);
            if (valid && other != null) DrawAssemblyGhost(other, -assemblyDragDelta, grid, size, border);
        }

        void DrawAssemblyGhost(List<int> piece, Vector2Int delta, Rect grid, float size, Color border)
        {
            foreach (int cell in piece)
            {
                Rect rect = CellRect(grid, size, cell % columns + delta.x, cell / columns + delta.y);
                Color fill = border;fill.a = .55f;
                EditorGUI.DrawRect(rect, fill);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), border);
                EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), border);
            }
        }
    }
}
