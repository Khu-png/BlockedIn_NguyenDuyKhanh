using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        static readonly Vector2Int[] connectionOffsets = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        readonly HashSet<int> completionColors = new HashSet<int>();
        readonly List<Block> completionGroup = new List<Block>();
        ColorCompletionEvaluator completionEvaluator;
        bool completionPending;
        float completionNotBefore;

        public void RequestCompletionCheck() => completionPending = true;

        public void OnDockFinished()
        {
            completionNotBefore = Mathf.Max(completionNotBefore, Time.time + .05f);
            RequestCompletionCheck();
        }

        public void OnBlocksMatched(Block first, Block second)
        {
            if (layout == null) return;
            PlayMatchSweep(first, second);
        }

        bool CanCheckCompletion()
        {
            return !PowerupBusy && Time.time >= completionNotBefore;
        }

        void LateUpdate()
        {
            TickPlacementPreview(Time.deltaTime);
            matchSweeps?.Tick(Time.deltaTime);
            if (completionPending) CheckCompletedColors();
        }

        public void CheckCompletedColors()
        {
            if (!Application.isPlaying || !CanCheckCompletion()) return;
            completionPending = false;
            completionEvaluator ??= new ColorCompletionEvaluator(WorldToGrid);
            completionColors.Clear();
            foreach (Block block in blocks)
                if (block != null && block.ColorData != null) completionColors.Add(block.ColorData.ColorId);
            foreach (int colorId in completionColors)
            {
                CollectColor(colorId);
                if (!completionEvaluator.IsComplete(completionGroup)) continue;
                // Exit animations own their snapshots while scratch buffers are reused.
                RemoveColor(new List<Block>(completionGroup));
            }
        }

        void CollectColor(int colorId)
        {
            completionGroup.Clear();
            foreach (Block block in blocks)
                if (block != null && block.ColorData != null && block.ColorData.ColorId == colorId) completionGroup.Add(block);
        }


    }
}

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        public event Action ColorsRemoved;
        public event Action FinalGoalExitStarted;
        public bool HasRemainingGoals
        {
            get
            {
                foreach (Block block in blocks)
                    if (HasPorts(block)) return true;
                return false;
            }
        }

        static bool HasPorts(Block block)
        {
            if (block == null) return false;
            foreach (var cell in block.Cells)
                for (int side = 0; side < 4; side++)
                {
                    EdgeType edge = block.EdgeAt(cell, (BlockSide)side);
                    if (edge == EdgeType.Tab || edge == EdgeType.Socket) return true;
                }
            return false;
        }
    }
}
