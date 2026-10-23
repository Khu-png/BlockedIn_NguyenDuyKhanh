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
            BlockEdgeData[] edges = null, int[] groups = null, Vector2Int[] goalPositions = null)
        {
            BlockShapeLayout.Validate(columns, colors, groups);
            if (colors != null && colors.Length != 0 && colors.Length != columns * rows)
                throw new InvalidOperationException("Block data must match the grid dimensions.");
            BoardGoalValidator.Validate(columns, rows, cells, colors, groups, goalPositions);
            if (edges != null && edges.Length != 0)
            {
                if (edges.Length != columns * rows) throw new InvalidOperationException("Edge data must match the grid dimensions.");
                foreach (BlockEdgeData entry in edges)
                    if (!entry.IsValid) throw new InvalidOperationException("Invalid block edge type.");
            }

            if (colors == null || colors.Length == 0) return;
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null) continue;
                if (colors[i].Material == null || cells[i] != BoardCell.Floor)
                    throw new InvalidOperationException("Each block needs a valid color and a floor cell.");
            }
        }

        public static void Generate(GeneratedBoard board, BlockColorData[] colors, Camera camera,
            BlockEdgeData[] edges = null, int[] groups = null, Vector2Int[] goalPositions = null)
        {
            Validate(board.columns, board.rows, board.cells, colors, edges, groups, goalPositions);
            Block prefab = HasBlocks(colors) ? BlockAssetBuilder.LoadBasePrefab() : null;
            Undo.RecordObject(board, "Update board blocks");
            if (board.blockGameplay != null) Undo.DestroyObjectImmediate(board.blockGameplay.gameObject);
            board.blockGameplay = null;
            board.SetInput(null);
            if (prefab == null) return;
            var root = new GameObject("Blocks");
            root.transform.SetParent(board.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Create board blocks");
            BlockBoard gameplay = Undo.AddComponent<BlockBoard>(root);
            gameplay.Configure(board, null, camera);
            gameplay.SetPlacementPreviewMaterial(AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/MainGame/Material/BlockPlacementPreview.mat"));
            board.blockGameplay = gameplay;
            BlockInput input = Undo.AddComponent<BlockInput>(root);
            input.Configure(camera, gameplay);
            board.SetInput(input);
            var spawned = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null || spawned.Contains(i)) continue;
                var members = BlockShapeLayout.Members(i, groups, colors);
                foreach (int cell in members) spawned.Add(cell);
                if (members.Count > 1)
                { BoardShapeBuilder.Add(gameplay, prefab, members, colors, edges, goalPositions); continue; }
                BlockEdgeData edge = edges != null && edges.Length > 0 ? edges[i] : default;
                Vector2Int position = new Vector2Int(i % board.columns, i / board.columns);
                Vector2Int goal = GoalAt(i, position, goalPositions);
                Add(gameplay, prefab, position, goal, colors[i], edge);
            }
            EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        static bool HasBlocks(BlockColorData[] colors)
        {
            if (colors == null) return false;
            foreach (BlockColorData color in colors) if (color != null) return true;
            return false;
        }

        static Vector2Int GoalAt(int index, Vector2Int fallback, Vector2Int[] goals)
        {
            return goals != null && goals.Length > index && goals[index] != PuzzlePieceMover.InvalidGoal
                ? goals[index] : fallback;
        }

        static void Add(BlockBoard board, Block prefab, Vector2Int position, Vector2Int goal,
            BlockColorData color, BlockEdgeData edges)
        {
            Block block = UnityEngine.Object.Instantiate(prefab, board.transform);
            block.name = "Block_" + position.x + "_" + position.y;
            Undo.RegisterCreatedObjectUndo(block.gameObject, "Place block");
            var data = new SerializedObject(block);
            data.FindProperty("gridPosition").vector2IntValue = position;
            data.FindProperty("colorData").objectReferenceValue = color;
            data.ApplyModifiedPropertiesWithoutUndo();
            block.SetGoalPosition(goal);
            edges.Apply(block.Appearance);
            BlockHitboxBuilder.Bake(block);
            board.Register(block);
        }
    }
}
