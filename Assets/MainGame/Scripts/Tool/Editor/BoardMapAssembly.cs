using System;
using System.Collections.Generic;
using BlockedIn.Blocks;
using BlockedIn.Blocks.Editor;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;
namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        [SerializeField] int assemblyStage;
        [SerializeField] BlockColorData assemblyColor;
        [SerializeField] int shuffleSteps = 80;
        string assemblyStatus;
        void DrawAssemblyTools()
        {
            EnsureGroups();
            editShape = assemblyStage != 0 || selectAssemblyGroups;
            string[] tips = {
                "Hold and drag to draw one complete group. Release to finish it. The next stroke creates a separate group, even beside the same color.",
                "Click the cells to cut off from one piece, then Cut Selected. Both new pieces must stay connected. Matching ports are created automatically.",
                "Drag a whole piece to reposition it. Green: free placement. Cyan: swap with one group. Red: blocked. Only the destination must be free; the drag can cross other pieces. Escape cancels. Right-click to edit Ice/Rope."
            };
            EditorGUILayout.HelpBox(tips[assemblyStage], MessageType.Info);
            if (assemblyStage == 0) DrawAssemblyMergeTools();
            if (assemblyStage == 1)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(selectedCells.Count == 0))
                        if (GUILayout.Button("Cut Selected")) Run(CutAssemblyPiece);
                    if (GUILayout.Button("Deselect")) selectedCells.Clear();
                }
                EditorGUILayout.LabelField("Selected cells: " + selectedCells.Count, EditorStyles.miniLabel);
            }
            if (assemblyStage == 2) DrawArrangement();
            if (!string.IsNullOrEmpty(assemblyStatus)) EditorGUILayout.HelpBox(assemblyStatus, MessageType.None);
        }
        void DrawAssemblyBrushes()
        {
            if (assemblyStage != 0) return;
            brushIndex = GUILayout.Toolbar(brushIndex, new[] { "Floor", "Wall", "Block" });
            if (brushIndex == 2)
            {
                if (assemblyColor == null) assemblyColor = BlockAssetBuilder.LoadColor("Blue");
                assemblyColor = (BlockColorData)EditorGUILayout.ObjectField("Shape color", assemblyColor, typeof(BlockColorData), false);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Floor")) Fill(MapTools.BoardCell.Floor);
                if (GUILayout.Button("Example 6x6")) Example();
            }
        }
        void SetAssemblyStage(int next)
        {
            CancelAssemblyDrag();
            if (next == 1 && assemblyStage == 0)
            {
                Undo.RecordObject(this, "Prepare complete puzzle shapes");
                PuzzleShapeCutter.Prepare(columns, blockColors, blockEdges, blockGroups);
            }
            Undo.RecordObject(this, "Change puzzle authoring step");
            selectAssemblyGroups = false;
            assemblyStage = next;editShape = next != 0;painting = false;
            selectedCells.Clear();assemblyStatus = null;
        }
        void CutAssemblyPiece()
        {
            Undo.RecordObject(this, "Cut puzzle piece");
            PuzzleShapeCutter.Cut(columns, blockColors, blockEdges, blockGroups, selectedCells);
            selectedCells.Clear();assemblyStatus = "Cut created two pieces with matching Tab/Socket edges.";
        }
        void DrawArrangement()
        {
            shuffleSteps = EditorGUILayout.IntSlider("Shuffle moves", shuffleSteps, 1, 300);
            if (GUILayout.Button("Shuffle with legal moves")) Run(ShuffleAssembly);
        }
        void MoveAssemblyPiece(Vector2Int delta)
        {
            if (selectedCells.Count == 0) return;
            List<int> piece = BlockShapeLayout.Members(selectedCells[0], blockGroups, blockColors);
            PlaceAssemblyPiece(piece, delta);
        }
        void ShuffleAssembly()
        {
            Undo.RecordObject(this, "Shuffle puzzle pieces");
            var lockedColors = new HashSet<int>();
            foreach (BlockIceData ice in frozenColors) if (ice.count > 0) lockedColors.Add(ice.colorId);
            int moved = PuzzlePieceMover.Shuffle(columns, cells, blockColors, blockEdges, blockGroups, ropeColors, shuffleSteps, lockedColors);
            selectedCells.Clear();
            assemblyStatus = moved == 0 ? "No legal shuffle move is available. Leave more free floor space."
                : "Applied " + moved + " legal moves. Play-test the result before using it as a level.";
        }
    }
}
