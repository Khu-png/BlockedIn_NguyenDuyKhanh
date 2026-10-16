using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        public bool CanSplit => colorData != null && !colorData.IsObstacle && CanDrag && !IsRopeCutting && cells.Length > 1
            && cellVisuals != null && cellVisuals.Length == cells.Length
            && cellAppearances != null && cellAppearances.Length == cells.Length
            && cellColliders != null && cellColliders.Length == cells.Length;

        public Block CreateSplitPart(IReadOnlyList<int> indices)
        {
            var root = new GameObject("SplitBlock");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform.parent, false);
            root.transform.SetPositionAndRotation(transform.position, transform.rotation);
            root.transform.localScale = transform.localScale;
            Block part = root.AddComponent<Block>();
            part.CopyDragSettings(this);
            Transform model = new GameObject("Visual").transform;
            model.SetParent(root.transform, false);
            var footprint = new Vector2Int[indices.Count];
            var appearances = new BlockAppearance[indices.Count];
            var colliders = new Collider[indices.Count];
            var models = new Transform[indices.Count];
            var icons = new SpriteRenderer[indices.Count];
            var body = new List<Renderer>();
            Vector2Int origin = cells[indices[0]];
            Vector3 shift = new Vector3(origin.x, 0f, -origin.y);
            root.transform.position = board.GridToWorld(gridPosition + origin);
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                footprint[i] = cells[index] - origin;
                models[i] = cellVisuals[index];
                foreach (Renderer renderer in renderers)
                    if (renderer != null && renderer.transform.IsChildOf(models[i])) body.Add(renderer);
                models[i].SetParent(model, false);
                models[i].localPosition -= shift;
                appearances[i] = cellAppearances[index];
                colliders[i] = cellColliders[index];
                colliders[i].transform.SetParent(root.transform, true);
                if (iconRenderers != null && index < iconRenderers.Length) icons[i] = iconRenderers[index];
            }
            part.Configure(colorData, gridPosition + origin, model, body.ToArray(), colliders[0], movable);
            part.ConfigureShape(footprint, appearances, colliders, models);
            part.iconRenderers = icons;
            part.RefreshSplitCorners();
            return part;
        }

        void RefreshSplitCorners()
        {
            int[] first = { 0, 0, 2, 2 };
            int[] second = { 3, 1, 1, 3 };
            for (int i = 0; i < cells.Length; i++)
                for (int corner = 0; corner < 4; corner++)
                {
                    bool joined = ContainsCell(cells[i] + sideOffsets[first[corner]])
                        || ContainsCell(cells[i] + sideOffsets[second[corner]]);
                    cellAppearances[i].SetCorner(corner, joined ? CornerType.CornerToStraight : CornerType.Default);
                }
        }

        public void SetSplitEdge(int index, int side, EdgeType edge)
            => cellAppearances[index].SetEdge((BlockSide)side, edge);
    }

    public sealed partial class BlockBoard
    {
        public System.Func<Block, bool> PowerupPress { get; set; }
        public bool PowerupBusy { get; set; }

        public Block[] SplitBlock(Block source)
        {
            if (source == null || !source.CanSplit || !blocks.Contains(source)) return null;
            for (int i = 0; i < source.Cells.Length; i++)
                for (int side = 0; side < 4; side++)
                {
                    int next = System.Array.IndexOf(source.Cells, source.Cells[i] + connectionOffsets[side]);
                    if (next < 0) continue;
                    source.SetSplitEdge(i, side, side < 2 ? EdgeType.Tab : EdgeType.Socket);
                }
            blocks.Remove(source);
            var parts = new Block[source.Cells.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = source.CreateSplitPart(new[] { i });
            source.enabled = false;
            foreach (Block part in parts) Register(part);
            Destroy(source.gameObject);
            return parts;
        }
    }
}
