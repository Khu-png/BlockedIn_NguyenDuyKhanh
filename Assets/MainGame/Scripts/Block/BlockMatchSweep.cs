using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        [SerializeField] Material matchSweepMaterial;
        MatchSweepPool matchSweeps;

        public void SetMatchSweepMaterial(Material material) => matchSweepMaterial = material;

        void PrepareMatchSweeps()
        {
            if (matchSweeps == null && matchSweepMaterial != null && layout != null)
                matchSweeps = new MatchSweepPool(layout.transform, layout.columns, layout.rows, matchSweepMaterial);
        }

        void PlayMatchSweep(Block first, Block second)
        {
            PrepareMatchSweeps();
            if (matchSweeps != null && matchSweeps.Play(first, second, blocks))
                completionNotBefore = Mathf.Max(completionNotBefore, Time.time + MatchSweepPool.Duration);
        }
    }
}
