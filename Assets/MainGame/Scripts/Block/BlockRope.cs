using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField] BlockRopeVisual ropeVisual;
        public bool IsRoped => ropeVisual != null && ropeVisual.IsLocked;
        public bool IsRopeCutting => ropeVisual != null && ropeVisual.IsCutting;
        public int RopeColorId => ropeVisual != null ? ropeVisual.ColorId : -1;
        public Camera RopeCamera => board != null ? board.ViewCamera : null;
        public void ConfigureRope(BlockRopeVisual value) => ropeVisual = value;
        public void CutRope(int colorId, Vector3? origin = null, float delay = 0f)
        {
            if (ropeVisual != null && ropeVisual.ColorId == colorId) ropeVisual.BeginCut(origin ?? transform.position, delay);
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
        void CutMatchingRopes(int colorId, Vector3 origin)
        {
            int index = 0;
            foreach (Block block in blocks)
                if (block != null && block.RopeColorId == colorId) block.CutRope(colorId, origin, index++ * .06f);
        }
        public void RopeCutCompleted()
        {
            RequestCompletionCheck();
            ColorsRemoved?.Invoke();
        }
    }
}
