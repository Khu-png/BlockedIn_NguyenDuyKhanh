using System.Collections;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockRopeVisual : MonoBehaviour
    {
        [SerializeField] Block owner;
        [SerializeField] BlockColorData color;
        [SerializeField] Transform[] halves;
        [SerializeField] Transform scissors;
        [SerializeField] AnimationClip cutAnimation;
        [SerializeField] Vector3 cutPoint;
        bool released, cutting;
        public int ColorId => color != null ? color.ColorId : -1;
        public bool IsLocked => !released;
        public bool IsCutting => cutting;

        public void Configure(Block block, BlockColorData ropeColor, Transform[] parts,
            Transform tool, AnimationClip animation, Vector3 point)
        {
            owner = block; color = ropeColor; halves = parts;
            scissors = tool; cutAnimation = animation; cutPoint = point;
            scissors.gameObject.SetActive(false);
        }
        public void BeginCut()
        {
            if (released || cutting) return;
            cutting = true;
            StartCoroutine(Cut());
        }
        IEnumerator Cut()
        {
            SoundManager.Play(SoundManager.RopeShowSound);
            scissors.gameObject.SetActive(true);
            Vector3 target = cutPoint + Vector3.up * .12f - Vector3.forward * .3f;
            Vector3 start = target + new Vector3(.7f, .8f, -.4f);
            float elapsed = 0f;
            while (elapsed < .5f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / .5f);
                scissors.localPosition = Vector3.Lerp(start, target, progress * progress * (3f - 2f * progress));
                scissors.localScale = Vector3.one * Mathf.Lerp(.15f, .85f, progress);
                yield return null;
            }
            elapsed = 0f;
            float duration = cutAnimation != null ? Mathf.Max(.1f, cutAnimation.length) : .6f;
            bool split = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                if (cutAnimation != null) cutAnimation.SampleAnimation(scissors.gameObject, Mathf.Min(elapsed, duration));
                if (!split && elapsed >= .32f) { split = true; SoundManager.Play(SoundManager.RopeCutSound); }
                if (split) SplitHalves(Mathf.Clamp01((elapsed - .32f) / .3f));
                scissors.localScale = Vector3.one * (.85f * (1f - Mathf.Clamp01((progress - .85f) / .15f)));
                yield return null;
            }
            foreach (Transform half in halves) if (half != null) half.gameObject.SetActive(false);
            scissors.gameObject.SetActive(false);
            released = true;
            cutting = false;
            owner.OnRopeCutComplete();
        }
        void SplitHalves(float progress)
        {
            for (int i = 0; i < halves.Length; i++)
            {
                if (halves[i] == null) continue;
                float side = i == 0 ? -1f : 1f;
                halves[i].localPosition = new Vector3(side * progress * .35f, -progress * progress * .3f, 0f);
                halves[i].localScale = Vector3.one * (1f - progress);
            }
        }
    }
}
