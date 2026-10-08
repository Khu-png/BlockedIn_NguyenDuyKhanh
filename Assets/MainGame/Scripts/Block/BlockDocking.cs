using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField, Min(.01f)] float dockDuration = .18f;
        [SerializeField, Min(.01f)] float dockBounceDuration = .16f;
        [SerializeField, Range(0, .15f)] float dockGap = .06f;
        [SerializeField, Range(0, .1f)] float dockSquash = .025f;
        static readonly Vector2Int[] sideOffsets = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        Block dockPartner;
        Vector2Int dockDirection;
        bool docking;
        float dockElapsed;
        Vector3 settleVisualTarget, dockStart, visualScale = Vector3.one;

        public bool IsDocking => docking;

        void PrepareDock()
        {
            dockPartner = FindDockPartner();
            settleVisualTarget = visualRest;
            if (dockPartner == null) return;
            Vector3 direction = board.GridToWorld(gridPosition + dockDirection) - board.GridToWorld(gridPosition);
            settleVisualTarget -= transform.InverseTransformVector(direction).normalized * dockGap;
        }

        Block FindDockPartner()
        {
            foreach (Vector2Int cell in cells)
                for (int side = 0; side < 4; side++)
                {
                    EdgeType first = EdgeAt(cell, (BlockSide)side);
                    if (first != EdgeType.Tab && first != EdgeType.Socket) continue;
                    Vector2Int target = gridPosition + cell + sideOffsets[side];
                    foreach (Block other in board.Blocks)
                    {
                        if (!CanDock(other) || !other.ContainsCell(target - other.GridPosition)) continue;
                        EdgeType second = other.EdgeAt(target - other.GridPosition, (BlockSide)((side + 2) % 4));
                        if (!IsPair(first, second)) continue;
                        dockDirection = sideOffsets[side];
                        return other;
                    }
                }
            return null;
        }

        bool CanDock(Block other)
        {
            return other != null && other != this && other.isActiveAndEnabled && HasSameColor(other)
                && !other.IsDragging && !other.IsSettling;
        }

        static bool IsPair(EdgeType first, EdgeType second)
        {
            return first == EdgeType.Tab && second == EdgeType.Socket
                || first == EdgeType.Socket && second == EdgeType.Tab;
        }

        void FinishSettle()
        {
            if (dockPartner == null || FindDockPartner() != dockPartner)
            {
                dockPartner = null;
                visual.localPosition = visualRest;
                return;
            }
            StartDock();
            dockPartner.StartDock();
            dockPartner = null;
        }

        void StartDock()
        {
            dockElapsed = 0;
            dockStart = visual.localPosition;
            docking = true;
        }

        void TickDock(float deltaTime)
        {
            if (!docking) return;
            dockElapsed += Mathf.Max(0, deltaTime);
            float slideTime = Mathf.Max(.01f, dockDuration);
            float bounceTime = Mathf.Max(.01f, dockBounceDuration);
            float slide = Mathf.Clamp01(dockElapsed / slideTime);
            float ease = 1 - Mathf.Pow(1 - slide, 3);
            visual.localPosition = Vector3.Lerp(dockStart, visualRest, ease);
            float bounce = Mathf.Clamp01((dockElapsed - slideTime) / bounceTime);
            float pulse = Mathf.Sin(bounce * Mathf.PI) * dockSquash;
            visual.localScale = Vector3.Scale(visualScale, new Vector3(1 + pulse, 1 - pulse, 1 + pulse));
            if (dockElapsed < slideTime + bounceTime) return;
            visual.localPosition = visualRest;
            visual.localScale = visualScale;
            docking = false;
        }
    }
}
