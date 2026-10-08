using System;
using BlockedIn.Blocks;
using BlockedIn.Blocks.Editor;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardBlockBuilder
    {
        public static void Validate(int columns, int rows, BoardCell[] cells, BlockColorData[] colors,
            BlockEdgeData[] edges = null, int[] groups = null)
        {
            BlockShapeLayout.Validate(columns, colors, groups);
            if (edges != null && edges.Length != 0)
            {
                if (edges.Length != columns * rows) throw new InvalidOperationException("Edge data must match the grid dimensions.");
                foreach (BlockEdgeData entry in edges)
                    if (!entry.IsValid) throw new InvalidOperationException("Invalid block edge type.");
            }
            if (colors == null || colors.Length == 0) return;
            if (colors.Length != columns * rows) throw new InvalidOperationException("Block data must match the grid dimensions.");
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null) continue;
                if (colors[i].Material == null || cells[i] != BoardCell.Floor)
                    throw new InvalidOperationException("Each block needs a valid color and a floor cell.");
            }
        }

        public static void Generate(GeneratedBoard board, BlockColorData[] colors, Camera camera,
            BlockEdgeData[] edges = null, int[] groups = null)
        {
            Validate(board.columns, board.rows, board.cells, colors, edges, groups);
            Block prefab = HasBlocks(colors) ? BlockAssetBuilder.LoadBasePrefab() : null;
            Undo.RecordObject(board, "Update board blocks");
            if (board.blockGameplay != null) Undo.DestroyObjectImmediate(board.blockGameplay.gameObject);
            board.blockGameplay = null;
            if (prefab == null) return;
            var root = new GameObject("Blocks");
            root.transform.SetParent(board.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Create board blocks");
            BlockBoard gameplay = Undo.AddComponent<BlockBoard>(root);
            gameplay.Configure(board, null);
            board.blockGameplay = gameplay;
            Undo.AddComponent<BlockInput>(root).Configure(camera, gameplay);
            var spawned = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null || spawned.Contains(i)) continue;
                var members = BlockShapeLayout.Members(i, groups, colors);
                foreach (int cell in members) spawned.Add(cell);
                if (members.Count > 1)
                { BoardShapeBuilder.Add(gameplay, prefab, members, colors, edges); continue; }
                BlockEdgeData edge = edges != null && edges.Length > 0 ? edges[i] : default;
                Add(gameplay, prefab, new Vector2Int(i % board.columns, i / board.columns), colors[i], edge);
            }
            EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        static bool HasBlocks(BlockColorData[] colors)
        {
            if (colors == null) return false;
            foreach (BlockColorData color in colors) if (color != null) return true;
            return false;
        }

        static void Add(BlockBoard board, Block prefab, Vector2Int position, BlockColorData color, BlockEdgeData edges)
        {
            Block block = UnityEngine.Object.Instantiate(prefab, board.transform);
            block.name = "Block_" + position.x + "_" + position.y;
            Undo.RegisterCreatedObjectUndo(block.gameObject, "Place block");
            var data = new SerializedObject(block);
            data.FindProperty("gridPosition").vector2IntValue = position;
            data.FindProperty("colorData").objectReferenceValue = color;
            data.ApplyModifiedPropertiesWithoutUndo();
            edges.Apply(block.Appearance);
            board.Register(block);
        }
    }
}
