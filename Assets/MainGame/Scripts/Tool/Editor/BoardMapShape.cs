using System;
using System.Collections.Generic;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed partial class BoardMapWindow
    {
        [SerializeField] int[] blockGroups;
        [SerializeField] bool editShape;
        [SerializeField] List<int> selectedCells = new List<int>();

        void EnsureGroups()
        {
            if (blockGroups == null || blockGroups.Length != columns * rows)
                blockGroups = new int[columns * rows];
            selectedCells.RemoveAll(i => i < 0 || i >= blockColors.Length || blockColors[i] == null);
        }

        void DrawShapeTools()
        {
            EnsureGroups();
            editShape = GUILayout.Toggle(editShape, "Edit Shape", "Button");
            if (!editShape) return;
            EditorGUILayout.HelpBox("Click block cells to select them. Merge joins touching cells with the same color ID. Add cells by painting blocks, then merge again. Split restores separate 1x1 blocks. Removing a cell that breaks the shape also splits the remaining cells.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selectedCells.Count == 0))
                {
                    if (GUILayout.Button("Merge")) Run(MergeSelected);
                    if (GUILayout.Button("Split")) Run(SplitSelected);
                    if (GUILayout.Button("Remove Cells")) Run(RemoveSelected);
                }
                if (GUILayout.Button("Deselect")) selectedCells.Clear();
            }
            EditorGUILayout.LabelField("Selected cells: " + selectedCells.Count, EditorStyles.miniLabel);
        }

        void SelectShapeCell(int index)
        {
            if (blockColors[index] == null) return;
            if (!selectedCells.Remove(index)) selectedCells.Add(index);
            Repaint();
        }

        List<int> ExpandedSelection()
        {
            var result = new HashSet<int>();
            foreach (int index in selectedCells)
                foreach (int cell in BlockShapeLayout.Members(index, blockGroups, blockColors)) result.Add(cell);
            var sorted = new List<int>(result);
            sorted.Sort();
            return sorted;
        }

        void MergeSelected()
        {
            List<int> members = ExpandedSelection();
            if (members.Count < 2) throw new InvalidOperationException("Select at least two block cells to merge.");
            if (!BlockShapeLayout.Connected(members, columns, cells.Length))
                throw new InvalidOperationException("Selected blocks must touch along their edges.");
            int colorId = blockColors[members[0]].ColorId;
            foreach (int index in members)
                if (blockColors[index].ColorId != colorId)
                    throw new InvalidOperationException("Choose the same color ID for all blocks before merging.");
            int id = ChooseGroupId(members);
            if (members.TrueForAll(index => blockGroups[index] == id))
            { selectedCells = members; Repaint(); return; }
            Undo.RecordObject(this, "Merge block shape");
            foreach (int index in members)
            {
                blockGroups[index] = id;
                blockColors[index] = blockColors[members[0]];
            }
            selectedCells = members;
            ClearInternalPorts(members);
            Repaint();
        }

        int ChooseGroupId(List<int> members)
        {
            int existing = int.MaxValue;
            foreach (int index in members)
                if (blockGroups[index] > 0) existing = Math.Min(existing, blockGroups[index]);
            if (existing != int.MaxValue) return existing;
            var used = new HashSet<int>(blockGroups);
            int id = 1;
            while (used.Contains(id)) id++;
            return id;
        }

        Dictionary<int, int> GroupLabels()
        {
            var ids = new SortedSet<int>();
            for (int i = 0; i < blockGroups.Length; i++)
                if (blockColors[i] != null && blockGroups[i] > 0) ids.Add(blockGroups[i]);
            var labels = new Dictionary<int, int>();
            foreach (int id in ids) labels.Add(id, labels.Count + 1);
            return labels;
        }

        void ClearInternalPorts(List<int> members)
        {
            var occupied = new HashSet<int>(members);
            foreach (int index in members)
                for (int side = 0; side < 4; side++)
                    if (occupied.Contains(BlockShapeLayout.Neighbor(index, side, columns, cells.Length)))
                        blockEdges[index].Set((Blocks.BlockSide)side, Blocks.EdgeType.Default);
        }

        void SplitSelected()
        {
            Undo.RecordObject(this, "Split block shape");
            foreach (int index in ExpandedSelection()) blockGroups[index] = 0;
            Repaint();
        }

        void RemoveSelected()
        {
            Undo.RecordObject(this, "Remove shape cells");
            foreach (int index in selectedCells)
            { blockColors[index] = null; blockEdges[index] = default; blockGroups[index] = 0; }
            selectedCells.Clear();
            RepairGroups();
            Repaint();
        }

        void RepairGroups()
        {
            var visited = new HashSet<int>();
            for (int i = 0; i < blockGroups.Length; i++)
            {
                if (blockColors[i] == null) { blockGroups[i] = 0; continue; }
                int id = blockGroups[i];
                if (id == 0 || !visited.Add(id)) continue;
                List<int> members = BlockShapeLayout.Members(i, blockGroups, blockColors);
                if (members.Count > 1 && BlockShapeLayout.Connected(members, columns, cells.Length)) continue;
                foreach (int cell in members) blockGroups[cell] = 0;
            }
        }

        bool[] InternalEdges(int index)
        {
            var internalEdges = new bool[4];
            if (blockGroups[index] == 0) return internalEdges;
            for (int side = 0; side < 4; side++)
            {
                int next = BlockShapeLayout.Neighbor(index, side, columns, cells.Length);
                internalEdges[side] = next >= 0 && blockColors[next] != null && blockGroups[next] == blockGroups[index];
            }
            return internalEdges;
        }
    }
}
