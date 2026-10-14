using System;
using BlockedIn.Blocks;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardRopeBuilder
    {
        public static void Validate(BlockColorData[] blocks, BlockColorData[] colors, int[] groups = null)
        {
            if (colors == null || colors.Length == 0) return;
            if (blocks == null || colors.Length != blocks.Length)
                throw new InvalidOperationException("Rope color data must match the grid dimensions.");
            var groupColors = new System.Collections.Generic.Dictionary<int, int>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == null) continue;
                if (blocks[i] == null || colors[i].Material == null)
                    throw new InvalidOperationException("Roped cells need a block and a valid Rope Color material.");
                if (blocks[i].ColorId == colors[i].ColorId)
                    throw new InvalidOperationException("Rope Color must differ from the locked block color.");
                if (groups == null || groups.Length != colors.Length || groups[i] == 0) continue;
                if (groupColors.TryGetValue(groups[i], out int id) && id != colors[i].ColorId)
                    throw new InvalidOperationException("All cells in a group must use the same Rope Color.");
                groupColors[groups[i]] = colors[i].ColorId;
            }
        }
        public static void Apply(BlockBoard board, BlockColorData[] colors)
        {
            if (board == null || colors == null || colors.Length == 0) return;
            if (colors.Length != board.Layout.columns * board.Layout.rows)
                throw new InvalidOperationException("Rope color data must match the grid dimensions.");
            foreach (Block block in board.Blocks)
            {
                BlockColorData color = null;
                foreach (Vector2Int cell in block.Cells)
                {
                    Vector2Int p = block.GridPosition + cell;
                    BlockColorData entry = colors[p.y * board.Layout.columns + p.x];
                    if (entry == null) continue;
                    if (color != null && color.ColorId != entry.ColorId)
                        throw new InvalidOperationException("All cells in a group must use the same Rope Color.");
                    color = entry;
                }
                if (color == null) continue;
                if (color.Material == null) throw new InvalidOperationException("Assign a material to the Rope Color.");
                if (color.ColorId == block.ColorData.ColorId)
                    throw new InvalidOperationException("Rope Color must differ from the locked block color.");
                Build(block, color);
            }
        }
        static void Build(Block block, BlockColorData color)
        {
            RopeAssetBuilder.EnsureFolder();
            var root = new GameObject("RopeVisual"); root.transform.SetParent(block.Visual, false);
            root.transform.localRotation = Quaternion.Inverse(block.Visual.localRotation);
            string name = color.name.Replace("BlockColor_", "");
            Material material = RopeAssetBuilder.Material("Rope_" + name, "textures/Rope_" + name, color.Material.shader);
            var halves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var model = new GameObject("RopeHalf_" + i); model.transform.SetParent(root.transform, false);
                MeshFilter filter = model.AddComponent<MeshFilter>(); filter.sharedMesh = BlockRopeMeshBuilder.Load(block.Cells, i);
                MeshRenderer renderer = model.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                halves[i] = model.transform;
            }
            Transform scissors = RopeAssetBuilder.Scissors(root.transform, material.shader);
            BlockRopeVisual visual = root.AddComponent<BlockRopeVisual>();
            Vector3 point = new Vector3(block.Cells[0].x, .52f, -block.Cells[0].y + .5f);
            AnimationClip clip = RopeAssetBuilder.CutClip(); clip.SampleAnimation(scissors.gameObject, 0f);
            visual.Configure(block, color, halves, scissors, clip, point);
            block.ConfigureRope(visual);
        }
    }
}
