using System.Collections.Generic;
using BlockedIn.Blocks;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardShapeBuilder
    {
        public static Block Add(BlockBoard board, Block prefab, List<int> members,
            BlockColorData[] colors, BlockEdgeData[] edges)
        {
            int columns = board.Layout.columns;
            int first = members[0];
            var origin = new Vector2Int(first % columns, first / columns);
            var root = new GameObject("Block_" + origin.x + "_" + origin.y + "_" + members.Count + "Cells");
            root.transform.SetParent(board.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Place merged block");
            Block block = Undo.AddComponent<Block>(root);
            block.CopyDragSettings(prefab);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var footprint = new Vector2Int[members.Count];
            var appearances = new BlockAppearance[members.Count];
            var colliders = new Collider[members.Count];
            var models = new Transform[members.Count];
            var renderers = new List<Renderer>();
            var occupied = new HashSet<int>(members);
            for (int i = 0; i < members.Count; i++)
            {
                int index = members[i];
                footprint[i] = new Vector2Int(index % columns, index / columns) - origin;
                Block cell = UnityEngine.Object.Instantiate(prefab, root.transform);
                cell.name = "Cell_" + footprint[i].x + "_" + footprint[i].y;
                Vector3 offset = new Vector3(footprint[i].x, 0, -footprint[i].y);
                cell.transform.localPosition = offset;
                cell.Visual.SetParent(visual, false);
                cell.Visual.localPosition += offset;
                models[i] = cell.Visual;
                appearances[i] = cell.Appearance;
                colliders[i] = cell.HitCollider;
                renderers.AddRange(cell.Renderers);
                BlockEdgeData ports = edges != null && edges.Length > 0 ? edges[index] : default;
                ApplyParts(cell.Appearance, index, occupied, columns, colors.Length, ports);
                BlockHitboxBuilder.Bake(cell);
                // Keep the user's prefab parts; only the shape root owns gameplay.
                UnityEngine.Object.DestroyImmediate(cell);
            }
            block.Configure(colors[first], origin, visual, renderers.ToArray(), colliders[0]);
            block.ConfigureShape(footprint, appearances, colliders, models);
            board.Register(block);
            return block;
        }

        static bool Joined(int index, int side, HashSet<int> occupied, int columns, int count)
        {
            return occupied.Contains(BlockShapeLayout.Neighbor(index, side, columns, count));
        }

        static void ApplyParts(BlockAppearance appearance, int index, HashSet<int> occupied,
            int columns, int count, BlockEdgeData ports)
        {
            for (int side = 0; side < 4; side++)
                appearance.SetEdge((BlockSide)side, Joined(index, side, occupied, columns, count)
                    ? EdgeType.Default : ports.Get((BlockSide)side));
            int[] firstSides = { 0, 0, 2, 2 };
            int[] secondSides = { 3, 1, 1, 3 };
            for (int corner = 0; corner < 4; corner++)
            {
                bool first = Joined(index, firstSides[corner], occupied, columns, count);
                bool second = Joined(index, secondSides[corner], occupied, columns, count);
                appearance.SetCorner(corner, first || second ? CornerType.CornerToStraight : CornerType.Default);
            }
        }

    }
}
