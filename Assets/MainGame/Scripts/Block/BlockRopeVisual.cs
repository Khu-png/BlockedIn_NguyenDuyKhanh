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
            if (effects != null) effects.PrepareRopeTrails(halves);
            Vector3 contact = cutPoint + Vector3.up * .12f;
            Quaternion cutRotation = FlightRotation(contact, .998f);
            // The blades extend along local -Z, so offset the pivot behind the cutting edge.
            Vector3 target = contact + cutRotation * Vector3.forward * .44f;
            Vector3 start = FlightPath(target, 0f);
            scissors.localPosition = start;
            if (effects != null) effects.StartFlight();
            float elapsed = 0f;
            const float flightDuration = 1.15f;
            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / flightDuration);
                float travel = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 position = FlightPath(target, travel);
                Vector3 heading = FlightPath(target, Mathf.Min(1f, travel + .006f)) - position;
                heading.y = 0f;
                Quaternion direction = heading.sqrMagnitude > .0001f ? Quaternion.LookRotation(-heading) : cutRotation;
                scissors.localRotation = direction;
                scissors.localPosition = position;
                scissors.localScale = Vector3.one * Mathf.Lerp(.32f, 1.08f, Mathf.Sin(progress * Mathf.PI * .5f));
                if (effects != null)
                {
                    effects.Flight(progress, Facing);
                    effects.Glow(.38f + Mathf.Sin(progress * Mathf.PI) * .3f, Facing);
                }
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
                if (!split && elapsed >= .22f) { split = true; SoundManager.Play(SoundManager.RopeCutSound); }
                if (split)
                {
                    SplitHalves(Mathf.Clamp01((elapsed - .22f) / .38f));
                    if (effects != null) effects.Burst(Mathf.Clamp01((elapsed - .22f) / .42f), transform.TransformPoint(cutPoint + Vector3.up * .14f), Facing);
                }
                if (effects != null) effects.Glow(.3f + Mathf.Exp(-Mathf.Abs(elapsed - .25f) * 18f) * .75f, Facing);
                yield return null;
            }
            foreach (Transform half in halves) if (half != null) half.gameObject.SetActive(false);
            elapsed = 0f; Vector3 exit = target + new Vector3(.95f, .8f, .5f);
            while (elapsed < .28f)
            {
                elapsed += Time.deltaTime; float progress = Mathf.Clamp01(elapsed / .28f);
                scissors.localPosition = Arc(target, target + new Vector3(.25f, .6f, -.1f), exit, progress);
                scissors.localRotation = cutRotation * Quaternion.Euler(progress * 15f, progress * 80f, 0f);
                scissors.localScale = Vector3.one * (1.08f * (1f - progress));
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
        static Vector3 FlightPath(Vector3 center, float value)
        {
            Vector3 start = center + new Vector3(-3.7f, .12f, -.9f);
            Vector3 lowLeft = center + new Vector3(-2.7f, .16f, -3.4f);
            Vector3 lowRight = center + new Vector3(3.05f, .16f, -3.25f);
            Vector3 outerRight = center + new Vector3(3.25f, .12f, 1.3f);
            if (value < .72f) return Cubic(start, lowLeft, lowRight, outerRight, value / .72f);
            Vector3 highRight = center + new Vector3(3.35f, .08f, 2.35f);
            Vector3 inward = center + new Vector3(1.65f, .04f, 1.05f);
            return Cubic(outerRight, highRight, inward, center, (value - .72f) / .28f);
        }
        static Quaternion FlightRotation(Vector3 target, float value)
        {
            Vector3 heading = FlightPath(target, Mathf.Min(1f, value + .002f)) - FlightPath(target, value);
            heading.y = 0f; return Quaternion.LookRotation(-heading.normalized, Vector3.up);
        }
        static Vector3 Cubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float value)
        {
            float inverse = 1f - value;
            return inverse * inverse * inverse * a + 3f * inverse * inverse * value * b +
                3f * inverse * value * value * c + value * value * value * d;
        }
    }
}
