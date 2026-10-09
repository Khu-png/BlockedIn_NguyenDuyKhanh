using System;
using System.Collections.Generic;
using BlockedIn.MapTools;
using BlockedIn.CameraTools;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard : MonoBehaviour
    {
        public const float PositionEpsilon = .0001f;
        [SerializeField] GeneratedBoard layout;
        [SerializeField] CameraManager cameraManager;
        [SerializeField] Camera viewCamera;
        [SerializeField] List<Block> blocks = new List<Block>();
        public IReadOnlyList<Block> Blocks => blocks;
        public GeneratedBoard Layout => layout;
        public Camera ViewCamera => cameraManager != null ? cameraManager.MainCamera : viewCamera;

        public void Configure(GeneratedBoard board, CameraManager cameraController, Camera camera = null)
        {
            layout = board;
            cameraManager = cameraController;
            if (camera != null) viewCamera = camera;
        }

        void Start()
        {
            foreach (Block block in blocks)
            {
                if (block == null || !CanPlace(block, block.GridPosition))
                    throw new InvalidOperationException("Invalid or overlapping block placement.");
                block.Initialize(this);
            }
            if (cameraManager != null) cameraManager.FocusLevel(layout);
            RequestCompletionCheck();
        }

        public void Register(Block block)
        {
            if (block == null || blocks.Contains(block) || !CanPlace(block, block.GridPosition))
                throw new InvalidOperationException("The block overlaps an obstacle or lies outside the board.");
            blocks.Add(block);
            block.Initialize(this);
            RequestCompletionCheck();
        }

        public Vector3 GridToWorld(Vector2 grid)
        {
            return layout.transform.TransformPoint(new Vector3(grid.x - (layout.columns - 1) * .5f,
                0, (layout.rows - 1) * .5f - grid.y));
        }

        public Vector2 WorldToGrid(Vector3 world)
        {
            Vector3 local = layout.transform.InverseTransformPoint(world);
            return new Vector2(local.x + (layout.columns - 1) * .5f, (layout.rows - 1) * .5f - local.z);
        }

        public bool IsBlocked(Vector2Int cell, Block moving)
        {
            if (layout == null || cell.x < 0 || cell.y < 0 || cell.x >= layout.columns || cell.y >= layout.rows)
                return true;
            if (layout.cells == null || layout.cells.Length != layout.columns * layout.rows)
                throw new InvalidOperationException("Board cell data does not match its dimensions.");
            if (layout.cells[cell.y * layout.columns + cell.x] != BoardCell.Floor) return true;
            foreach (Block other in blocks)
            {
                if (other == null || other == moving) continue;
                foreach (Vector2Int offset in other.Cells)
                    if (other.GridPosition + offset == cell) return true;
            }
            return false;
        }

        public bool CanPlace(Block block, Vector2Int origin)
        {
            if (block.Cells == null || block.Cells.Length == 0) return false;
            foreach (Vector2Int offset in block.Cells)
                if (IsBlocked(origin + offset, block)) return false;
            return true;
        }

        public Vector2 Constrain(Block block, Vector2 current, Vector2 target)
        {
            Vector2 delta = target - current;
            return Slide(block, current, target, Mathf.Abs(delta.x) >= Mathf.Abs(delta.y));
        }

        float LimitAxis(Block block, Vector2 current, float target, bool horizontal)
        {
            float start = horizontal ? current.x : current.y;
            bool positive = target > start;
            int length = horizontal ? layout.columns : layout.rows;
            float result = target;
            foreach (Vector2Int offset in block.Cells)
            {
                float axisOffset = horizontal ? offset.x : offset.y;
                float cross = horizontal ? current.y + offset.y : current.x + offset.x;
                result = Mathf.Clamp(result, -axisOffset, length - 1 - axisOffset);
                int first = Mathf.FloorToInt(cross + PositionEpsilon);
                int last = Mathf.CeilToInt(cross - PositionEpsilon);
                for (int row = first; row <= last; row++)
                    for (int index = 0; index < length; index++)
                    {
                        Vector2Int cell = horizontal ? new Vector2Int(index, row) : new Vector2Int(row, index);
                        if (!IsBlocked(cell, block)) continue;
                        float boundary = index - axisOffset + (positive ? -1 : 1);
                        if (positive && boundary >= start - PositionEpsilon) result = Mathf.Min(result, boundary);
                        if (!positive && boundary <= start + PositionEpsilon) result = Mathf.Max(result, boundary);
                    }
            }
            return result;
        }

        public bool TrySnap(Block block, Vector2 current, float tolerance, out Vector2Int destination)
        {
            destination = Vector2Int.RoundToInt(current);
            if (Vector2.Distance(current, destination) <= tolerance && CanReach(block, current, destination)) return true;
            float best = float.PositiveInfinity;
            for (int x = Mathf.FloorToInt(current.x); x <= Mathf.CeilToInt(current.x); x++)
                for (int y = Mathf.FloorToInt(current.y); y <= Mathf.CeilToInt(current.y); y++)
                {
                    var candidate = new Vector2Int(x, y);
                    float distance = ((Vector2)candidate - current).sqrMagnitude;
                    if (distance >= best || !CanReach(block, current, candidate)) continue;
                    best = distance;
                    destination = candidate;
                }
            return !float.IsPositiveInfinity(best);
        }

        bool CanReach(Block block, Vector2 current, Vector2Int destination)
        {
            if (!CanPlace(block, destination)) return false;
            return Vector2.Distance(Constrain(block, current, destination), destination) < PositionEpsilon;
        }

        public Block Resolve(Collider collider)
        {
            foreach (Block block in blocks)
                if (block != null && block.OwnsCollider(collider)) return block;
            return null;
        }
    }
}
