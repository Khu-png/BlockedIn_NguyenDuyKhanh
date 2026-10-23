using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField, Min(.01f)] float dockDuration = .12f;
        [SerializeField, Range(0f, .075f)] float dockPortTravel = .065f;
        static readonly Vector2Int[] sideOffsets = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        readonly List<DockPort> dockPorts = new List<DockPort>();
        readonly HashSet<Block> dockPartners = new HashSet<Block>();
        Block dockPartner;
        bool docking;
        float dockElapsed;
        float matchPulseElapsed;
        bool matchPulseActive;
        Vector3 settleVisualTarget, visualScale = Vector3.one;

        struct DockPort
        {
            public BlockAppearance appearance;
            public BlockSide side;
            public Vector3 direction;
        }

        public bool IsDocking => docking;

        void PrepareDock()
        {
            dockPartner = FindDockPartner();
            settleVisualTarget = visualRest;
            if (dockPartner == null) return;
            for (int index = 0; index < cells.Length; index++)
                for (int side = 0; side < 4; side++)
                {
                    if (!TryDockNeighbor(cells[index], side, out Block other)) continue;
                    if (EdgeAt(cells[index], (BlockSide)side) == EdgeType.Tab)
                        RetractPort(index, side);
                    else
                    {
                        Vector2Int target = gridPosition + cells[index] + sideOffsets[side] - other.GridPosition;
                        other.RetractPort(System.Array.IndexOf(other.cells, target), (side + 2) % 4);
                    }
                }
        }

        Block FindDockPartner()
        {
            foreach (Vector2Int cell in cells)
                for (int side = 0; side < 4; side++)
                    if (TryDockNeighbor(cell, side, out Block other)) return other;
            return null;
        }

        bool TryDockNeighbor(Vector2Int cell, int side, out Block partner)
        {
            partner = null;
            EdgeType edge = EdgeAt(cell, (BlockSide)side);
            if (edge != EdgeType.Tab && edge != EdgeType.Socket) return false;
            Vector2Int target = gridPosition + cell + sideOffsets[side];
            foreach (Block other in board.Blocks)
            {
                if (!CanDock(other) || !other.ContainsCell(target - other.GridPosition)) continue;
                EdgeType opposite = other.EdgeAt(target - other.GridPosition, (BlockSide)((side + 2) % 4));
                if (!IsPair(edge, opposite)) continue;
                partner = other;
                return true;
            }
            return false;
        }

        bool CanDock(Block other)
        {
            return other != null && other.isActiveAndEnabled && BlockConnectionRules.CanMatch(this, other)
                && !other.IsFrozen && !other.IsRoped && !other.IsRopeCutting && !other.IsDragging
                && !other.settling && Vector2.Distance(board.WorldToGrid(other.transform.position),
                    other.GridPosition) <= BlockBoard.PositionEpsilon;
        }

        static bool IsPair(EdgeType first, EdgeType second)
        {
            return first == EdgeType.Tab && second == EdgeType.Socket
                || first == EdgeType.Socket && second == EdgeType.Tab;
        }

        void FinishSettle()
        {
            board.RequestCompletionCheck();
            if (dockPartner == null || FindDockPartner() == null)
            {
                dockPartner = null;
                visual.localPosition = visualRest;
                return;
            }
            dockPartners.Clear();
            foreach (Vector2Int cell in cells)
                for (int side = 0; side < 4; side++)
                    if (TryDockNeighbor(cell, side, out Block other))
                    {
                        if (dockPartners.Add(other))
                            board.OnBlocksMatched(this, other);
                    }
            StartMatchPulse();
            foreach (Block partner in dockPartners)
            {
                partner.StartMatchPulse();
                partner.StartDock();
            }
            StartDock();
            SoundManager.Play(SoundManager.MatchSound);
            dockPartner = null;
        }

        void RetractPort(int index, int side)
        {
            BlockAppearance part = cellAppearances != null && cellAppearances.Length == cells.Length
                ? cellAppearances[index] : appearance;
            Vector3 direction = board.GridToWorld(gridPosition + sideOffsets[side]) - board.GridToWorld(gridPosition);
            if (part != null) part.SetTabRetraction((BlockSide)side, direction, dockPortTravel);
        }

        void StartDock()
        {
            dockPorts.Clear();
            for (int index = 0; index < cells.Length; index++)
                for (int side = 0; side < 4; side++)
                {
                    if (EdgeAt(cells[index], (BlockSide)side) != EdgeType.Tab
                        || !TryDockNeighbor(cells[index], side, out _)) continue;
                    BlockAppearance part = cellAppearances != null && cellAppearances.Length == cells.Length
                        ? cellAppearances[index] : appearance;
                    Vector3 direction = board.GridToWorld(gridPosition + sideOffsets[side])
                        - board.GridToWorld(gridPosition);
                    dockPorts.Add(new DockPort { appearance = part, side = (BlockSide)side, direction = direction });
                }
            dockElapsed = 0f;
            docking = true;
        }

        void StartMatchPulse()
        {
            matchPulseElapsed = 0;
            matchPulseActive = true;
        }

        void TickDock(float deltaTime)
        {
            if (docking)
            {
                dockElapsed += Mathf.Max(0f, deltaTime);
                float progress = Mathf.Clamp01(dockElapsed / Mathf.Max(.01f, dockDuration));
                float ease = progress * progress * (3f - 2f * progress);
                foreach (DockPort port in dockPorts)
                    if (port.appearance != null)
                        port.appearance.SetTabRetraction(port.side, port.direction, dockPortTravel * (1f - ease));
                if (progress >= 1f)
                {
                    docking = false;
                    board.OnDockFinished();
                }
            }
            TickMatchPulse(deltaTime);
        }

        void TickMatchPulse(float deltaTime)
        {
            if (!matchPulseActive || visual == null) return;
            const float duration = .24f;
            matchPulseElapsed += Mathf.Max(0, deltaTime);
            float progress = Mathf.Clamp01(matchPulseElapsed / duration);
            float horizontal, vertical;
            if (progress < .25f)
            {
                float t = progress / .25f;
                horizontal = Mathf.Lerp(1, .98f, t);
                vertical = Mathf.Lerp(1, 1.02f, t);
            }
            else if (progress < .62f)
            {
                float t = (progress - .25f) / .37f;
                horizontal = Mathf.Lerp(.98f, 1.02f, t);
                vertical = Mathf.Lerp(1.02f, .99f, t);
            }
            else
            {
                float t = (progress - .62f) / .38f;
                horizontal = Mathf.Lerp(1.02f, 1, t);
                vertical = Mathf.Lerp(.99f, 1, t);
            }
            visual.localScale = Vector3.Scale(visualScale, new Vector3(horizontal, vertical, horizontal));
            if (progress < 1) return;
            visual.localScale = visualScale;
            matchPulseActive = false;
        }

        public void RefreshDockPorts()
        {
            if (IsSettling) return;
            dockPorts.Clear();
            if (cellAppearances != null && cellAppearances.Length > 0)
            {
                foreach (BlockAppearance part in cellAppearances)
                    if (part != null) part.ResetTabExtensions();
            }
            else if (appearance != null) appearance.ResetTabExtensions();
            if (board == null || !isActiveAndEnabled || IsDragging || IsSettling || exiting || IsFrozen || IsRoped) return;
            StartDock();
            TickDock(dockDuration);
        }
    }

}
