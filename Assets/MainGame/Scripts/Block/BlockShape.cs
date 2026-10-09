using System;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField, HideInInspector] BlockAppearance[] cellAppearances;
        [SerializeField, HideInInspector] Collider[] cellColliders;
        [SerializeField, HideInInspector] Transform[] cellVisuals;
        public Transform Visual => visual;
        public Renderer[] Renderers => renderers;
        bool exiting;
        public int ExitVisualCount => cellVisuals != null && cellVisuals.Length == cells.Length ? cellVisuals.Length : 1;
        public Transform GetExitVisual(int index) => ExitVisualCount == 1 ? visual : cellVisuals[index];

        public void BeginExit()
        {
            exiting = true;
            dragging = settling = docking = false;
            ResetTilt();
            if (cells.Length > 1 && cellAppearances != null)
                foreach (BlockAppearance part in cellAppearances)
                    if (part != null)
                        for (int corner = 0; corner < 4; corner++) part.SetCorner(corner, CornerType.Default);
            if (hitCollider != null) hitCollider.enabled = false;
            if (cellColliders == null) return;
            foreach (Collider collider in cellColliders)
                if (collider != null) collider.enabled = false;
        }

        public void ConfigureShape(Vector2Int[] footprint, BlockAppearance[] parts, Collider[] colliders,
            Transform[] models = null)
        {
            if (footprint == null || parts == null || colliders == null || footprint.Length == 0
                || footprint.Length != parts.Length || footprint.Length != colliders.Length)
                throw new ArgumentException("Assign one appearance and collider per shape cell.");
            cells = footprint;
            cellAppearances = parts;
            cellColliders = colliders;
            cellVisuals = models;
            appearance = parts[0];
            hitCollider = colliders[0];
        }

        public void SetHitCollider(Collider collider)
        {
            Collider previous = hitCollider;
            hitCollider = collider;
            if (cellColliders == null) return;
            for (int i = 0; i < cellColliders.Length; i++)
                if (cellColliders[i] == previous) cellColliders[i] = collider;
        }

        public bool OwnsCollider(Collider candidate)
        {
            if (candidate == null) return false;
            if (cellColliders == null || cellColliders.Length == 0) return candidate == hitCollider;
            foreach (Collider item in cellColliders) if (item == candidate) return true;
            return false;
        }

        public bool ContainsCell(Vector2Int offset)
        {
            foreach (Vector2Int cell in cells) if (cell == offset) return true;
            return false;
        }

        public EdgeType EdgeAt(Vector2Int offset, BlockSide side)
        {
            if (ContainsCell(offset + sideOffsets[(int)side])) return EdgeType.None;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != offset) continue;
                BlockAppearance part = cellAppearances != null && cellAppearances.Length == cells.Length
                    ? cellAppearances[i] : appearance;
                return part != null ? part.GetEdge(side) : EdgeType.None;
            }
            return EdgeType.None;
        }
    }
}
