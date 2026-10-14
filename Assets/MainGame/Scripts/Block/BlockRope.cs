using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField] BlockRopeVisual ropeVisual;
        public bool IsRoped => ropeVisual != null && ropeVisual.IsLocked;
        public bool IsRopeCutting => ropeVisual != null && ropeVisual.IsCutting;
        public void ConfigureRope(BlockRopeVisual value) => ropeVisual = value;
        public void CutRope(int colorId)
        {
            if (ropeVisual != null && ropeVisual.ColorId == colorId) ropeVisual.BeginCut();
        }
        public void OnRopeCutComplete()
        {
            if (board != null) board.RopeCutCompleted();
        }
    }
    public sealed partial class BlockBoard
    {
        public bool HasRopesCutting
        {
            get
            {
                foreach (Block block in blocks)
                    if (block != null && block.IsRopeCutting) return true;
                return false;
            }
        }
        void CutMatchingRopes(int colorId)
        {
            foreach (Block block in blocks) if (block != null) block.CutRope(colorId);
        }
        public void RopeCutCompleted()
        {
            RequestCompletionCheck();
            ColorsRemoved?.Invoke();
        }
    }
}
