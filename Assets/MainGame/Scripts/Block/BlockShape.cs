using System;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField] BlockAppearance[] cellAppearances;
        [SerializeField] Collider[] cellColliders;
        public Transform Visual => visual;
        public Renderer[] Renderers => renderers;

        public void ConfigureShape(Vector2Int[] footprint, BlockAppearance[] parts, Collider[] colliders)
        {
            if (footprint == null || parts == null || colliders == null || footprint.Length == 0
                || footprint.Length != parts.Length || footprint.Length != colliders.Length)
                throw new ArgumentException("Assign one appearance and collider per shape cell.");
            cells = footprint;
            cellAppearances = parts;
            cellColliders = colliders;
            appearance = parts[0];
            hitCollider = colliders[0];
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
