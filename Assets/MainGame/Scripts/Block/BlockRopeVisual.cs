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
        [SerializeField] BlockRopeEffects effects;
        bool released, cutting;
        public int ColorId => color != null ? color.ColorId : -1;
        public bool IsLocked => !released;
        public bool IsCutting => cutting;

        public void Configure(Block block, BlockColorData ropeColor, Transform[] parts,
            Transform tool, AnimationClip animation, Vector3 point, BlockRopeEffects feedback = null)
        {
            owner = block; color = ropeColor; halves = parts;
            scissors = tool; cutAnimation = animation; cutPoint = point;
            effects = feedback;
            scissors.gameObject.SetActive(false);
        }
        public void BeginCut(Vector3 origin, float delay = 0f)
        {
            if (released || cutting) return;
            cutting = true;
            StartCoroutine(Cut(origin, delay));
        }
        Quaternion Facing => owner.RopeCamera != null ? owner.RopeCamera.transform.rotation : Quaternion.Euler(90f, 0f, 0f);
        IEnumerator Cut(Vector3 origin, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            SoundManager.Play(SoundManager.RopeShowSound);
            scissors.gameObject.SetActive(true);
            Quaternion cutRotation = Quaternion.Euler(0f, 45f, 0f);
            // The blades extend along local -Z; the handles extend along +Z.
            Vector3 target = cutPoint + Vector3.up * .12f + cutRotation * Vector3.forward * .28f;
            Vector3 start = transform.InverseTransformPoint(origin) + new Vector3(.15f, 1f, -.2f);
            scissors.localPosition = start;
            if (effects != null) effects.StartFlight();
            Vector3 bend = (start + target) * .5f + new Vector3(.65f, .7f, .35f);
            float elapsed = 0f;
            while (elapsed < .5f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / .5f);
                float travel = 1f - Mathf.Pow(1f - progress, 2f);
                Vector3 position = Arc(start, bend, target, travel);
                Vector3 heading = position - scissors.localPosition; heading.y = 0f;
                Quaternion direction = heading.sqrMagnitude > .0001f ? Quaternion.LookRotation(-heading) : cutRotation;
                scissors.localRotation = Quaternion.Slerp(direction, cutRotation,
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.6f, 1f, progress)));
                scissors.localPosition = position;
                scissors.localScale = Vector3.one * Mathf.Lerp(.25f, .85f, Mathf.Sin(progress * Mathf.PI * .5f));
                if (effects != null) effects.Glow(.18f + Mathf.Sin(progress * Mathf.PI) * .12f, Facing);
                yield return null;
            }
            elapsed = 0f;
            const float duration = .66f;
            bool split = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                if (cutAnimation != null) cutAnimation.SampleAnimation(scissors.gameObject, Mathf.Min(elapsed, duration));
                if (!split && elapsed >= .32f) { split = true; SoundManager.Play(SoundManager.RopeCutSound); }
                if (split)
                {
                    SplitHalves(Mathf.Clamp01((elapsed - .32f) / .3f));
                    if (effects != null) effects.Burst(Mathf.Clamp01((elapsed - .32f) / .34f), transform.TransformPoint(cutPoint + Vector3.up * .14f), Facing);
                }
                if (effects != null) effects.Glow(.15f + Mathf.Exp(-Mathf.Abs(elapsed - .32f) * 22f) * .4f, Facing);
                yield return null;
            }
            foreach (Transform half in halves) if (half != null) half.gameObject.SetActive(false);
            elapsed = 0f; Vector3 exit = target + new Vector3(.95f, .8f, .5f);
            while (elapsed < .28f)
            {
                elapsed += Time.deltaTime; float progress = Mathf.Clamp01(elapsed / .28f);
                scissors.localPosition = Arc(target, target + new Vector3(.25f, .6f, -.1f), exit, progress);
                scissors.localRotation = cutRotation * Quaternion.Euler(progress * 15f, progress * 80f, 0f);
                scissors.localScale = Vector3.one * (.85f * (1f - progress));
                if (effects != null)
                {
                    effects.Glow(.15f * (1f - progress), Facing);
                    if (progress > .6f) effects.StopFlight();
                }
                yield return null;
            }
            if (effects != null) effects.StopFlight();
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
                halves[i].localPosition = cutPoint + new Vector3(side * progress * .35f, -progress * progress * .3f, 0f);
                halves[i].localScale = Vector3.one * (1f - progress);
            }
        }
        static Vector3 Arc(Vector3 start, Vector3 bend, Vector3 end, float t)
            => (1f - t) * (1f - t) * start + 2f * (1f - t) * t * bend + t * t * end;
    }
}
