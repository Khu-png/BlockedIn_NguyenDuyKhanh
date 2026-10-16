using System;
using System.Collections.Generic;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        [SerializeField] bool selectAssemblyGroups;

        void DrawAssemblyMergeTools()
        {
            bool next = GUILayout.Toggle(selectAssemblyGroups, "Select Groups to Merge", "Button");
            if (next != selectAssemblyGroups)
            {
                selectAssemblyGroups = next;
                editShape = next;
                painting = false;
                selectedCells.Clear();
            }
            if (!selectAssemblyGroups) return;
            EditorGUILayout.HelpBox("Click groups to select or deselect them. Merge Groups joins whole groups of the same color that touch along their edges. Right-click to edit a block.", MessageType.Info);
            int count = SelectedAssemblyGroupCount();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(count < 2))
                    if (GUILayout.Button("Merge Groups")) Run(MergeAssemblyGroups);
                if (GUILayout.Button("Deselect")) selectedCells.Clear();
            }
            EditorGUILayout.LabelField("Selected groups: " + count, EditorStyles.miniLabel);
        }

        void ToggleAssemblyGroup(int index)
        {
            if (blockColors[index] == null) return;
            List<int> members = BlockShapeLayout.Members(index, blockGroups, blockColors);
            bool remove = members.TrueForAll(selectedCells.Contains);
            foreach (int cell in members)
            {
                if (remove) selectedCells.Remove(cell);
                else if (!selectedCells.Contains(cell)) selectedCells.Add(cell);
            }
            Repaint();
        }

        int SelectedAssemblyGroupCount()
        {
            var groups = new HashSet<int>();
            foreach (int cell in selectedCells)
                if (blockColors[cell] != null) groups.Add(blockGroups[cell] > 0 ? blockGroups[cell] : -cell - 1);
            return groups.Count;
        }

        void MergeAssemblyGroups()
        {
            if (SelectedAssemblyGroupCount() < 2)
                throw new InvalidOperationException("Select at least two groups to merge.");
            MergeSelected();
            assemblyStatus = "Selected groups merged. Disable Select Groups to Merge to resume painting.";
        }
    }
}
