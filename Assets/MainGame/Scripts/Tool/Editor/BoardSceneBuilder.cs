using System;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockedIn.EditorTools
{
    public static class BoardSceneBuilder
    {
        public static GeneratedBoard Generate(GeneratedBoard target, int columns, int rows,
            bool border, BoardCell[] cells, GameObject tile, GameObject backdrop, GameObject wall)
        {
            ValidateScene(target);
            ValidateMap(columns, rows, cells);
            ValidatePrefab(tile);
            ValidatePrefab(backdrop);
            ValidatePrefab(wall);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate board");
            target = Prepare(target);
            SetData(target, columns, rows, border, cells);
            FillCells(target, tile, backdrop, wall);
            if (border) AddBorder(target, wall);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            Selection.activeGameObject = target.gameObject;
            return target;
        }

        static void ValidateScene(GeneratedBoard target)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) throw new InvalidOperationException("Open a scene instead of Prefab Mode.");
            if (target == null) return;
            if (EditorUtility.IsPersistent(target) || target.gameObject.scene != SceneManager.GetActiveScene())
                throw new InvalidOperationException("Select a board in the active scene.");
        }

        static void ValidateMap(int columns, int rows, BoardCell[] cells)
        {
            if (columns < 1 || columns > 32 || rows < 1 || rows > 32 || cells == null || cells.Length != columns * rows)
                throw new InvalidOperationException("Map dimensions must be between 1 and 32 with matching cell data.");
            foreach (BoardCell cell in cells)
                if (cell < BoardCell.Empty || cell > BoardCell.Wall) throw new InvalidOperationException("Invalid cell type.");
        }

        static void ValidatePrefab(GameObject prefab)
        {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException("Assign valid Tile, Backdrop and Wall prefabs in the tool.");
        }

        static GeneratedBoard Prepare(GeneratedBoard target)
        {
            if (target == null)
            {
                var root = new GameObject("GeneratedMap");
                Undo.RegisterCreatedObjectUndo(root, "Create board");
                target = Undo.AddComponent<GeneratedBoard>(root);
            }
            Undo.RecordObject(target, "Update board groups");
            RemoveGroup(target.Tiles);
            RemoveGroup(target.Backdrop);
            RemoveGroup(target.Walls);
            target.SetGroups(CreateGroup(target, "Tiles"), CreateGroup(target, "Backdrop"), CreateGroup(target, "Walls"));
            return target;
        }

        static Transform CreateGroup(GeneratedBoard board, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(board.transform, false);
            Undo.RegisterCreatedObjectUndo(group, "Create board group");
            return group.transform;
        }

        static void RemoveGroup(Transform group)
        {
            if (group != null) Undo.DestroyObjectImmediate(group.gameObject);
        }

        static void SetData(GeneratedBoard board, int columns, int rows, bool border, BoardCell[] cells)
        {
            board.columns = columns;
            board.rows = rows;
            board.outerWalls = border;
            board.cells = (BoardCell[])cells.Clone();
        }

        static void FillCells(GeneratedBoard board, GameObject tile, GameObject backdrop, GameObject wall)
        {
            for (int y = 0; y < board.rows; y++)
                for (int x = 0; x < board.columns; x++)
                {
                    BoardCell cell = board.cells[y * board.columns + x];
                    if (cell == BoardCell.Empty) continue;
                    Place(board, backdrop, board.Backdrop, x, y, -.225f);
                    if (cell == BoardCell.Floor) Place(board, tile, board.Tiles, x, y, -.22f);
                    else Place(board, wall, board.Walls, x, y, .03f);
                }
        }

        static void AddBorder(GeneratedBoard board, GameObject wall)
        {
            for (int x = -1; x <= board.columns; x++)
            {
                Place(board, wall, board.Walls, x, -1, .03f);
                Place(board, wall, board.Walls, x, board.rows, .03f);
            }
            for (int y = 0; y < board.rows; y++)
            {
                Place(board, wall, board.Walls, -1, y, .03f);
                Place(board, wall, board.Walls, board.columns, y, .03f);
            }
        }

        static void Place(GeneratedBoard board, GameObject prefab, Transform group, int x, int y, float elevation)
        {
            var cell = (GameObject)PrefabUtility.InstantiatePrefab(prefab, board.gameObject.scene);
            cell.name = prefab.name + "_" + x + "_" + y;
            cell.transform.SetParent(group, false);
            cell.transform.localPosition = new Vector3(x - (board.columns - 1) * .5f, elevation, (board.rows - 1) * .5f - y);
            Undo.RegisterCreatedObjectUndo(cell, "Place board cell");
        }

        public static void Clear(GeneratedBoard board)
        {
            if (board == null) return;
            ValidateScene(board);
            var scene = board.gameObject.scene;
            Undo.DestroyObjectImmediate(board.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
