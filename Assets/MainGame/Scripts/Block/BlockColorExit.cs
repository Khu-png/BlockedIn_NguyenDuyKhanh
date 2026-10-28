using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        [SerializeField, Min(.1f)] float colorExitDuration = .95f;
        [SerializeField, Min(0)] float colorExitHeight = 1.5f;
        [SerializeField, Min(0)] float colorExitSpin = 60f;
        const float PickupDuration = .14f;
        const float PickupDistance = .22f;
        const float PickupScale = 1.22f;
        int removingColors;
        public bool IsRemovingColors => removingColors > 0 || HasRopesCutting;

        struct ExitVisual
        {
            public Transform model;
            public Vector3 start, scale;
            public Quaternion rotation;
            public Vector2 spread;
        }

        void RemoveColor(List<Block> group)
        {
            Camera camera = ViewCamera;
            foreach (Block block in group)
            {
                blocks.Remove(block);
                block.BeginExit();
            }
            removingColors++;
            SoundManager.Play(SoundManager.BreakSound);
            ThawOtherColors(group[0].ColorData.ColorId);
            CutMatchingRopes(group[0].ColorData.ColorId, group[0].Visual.position);
            StartCoroutine(PlayColorExit(group, camera));
        }

        List<ExitVisual> CaptureExitVisuals(List<Block> group, Camera camera)
        {
            var visuals = new List<ExitVisual>();
            foreach (Block block in group)
            {
                block.RestoreExitCorners();
                for (int i = 0; i < block.ExitVisualCount; i++)
                {
                    Transform model = block.GetExitVisual(i);
                    Vector3 start = camera != null ? camera.WorldToViewportPoint(model.position) : model.position;
                    visuals.Add(new ExitVisual { model = model, start = start, scale = model.localScale,
                        rotation = model.rotation });
                }
            }
            SetExitSpread(visuals, camera);
            return visuals;
        }

        void SetExitSpread(List<ExitVisual> visuals, Camera camera)
        {
            Vector2 center = Vector2.zero;
            foreach (ExitVisual item in visuals) center += ExitPlanePosition(item.start, camera);
            center /= Mathf.Max(1, visuals.Count);
            float horizontalSpread = 1.6f;
            if (camera != null)
                foreach (ExitVisual item in visuals)
                {
                    float offset = item.start.x - center.x;
                    if (Mathf.Abs(offset) < .0001f) continue;
                    float edge = offset > 0 ? .93f : .07f;
                    horizontalSpread = Mathf.Min(horizontalSpread, Mathf.Max(0, (edge - item.start.x) / offset));
                }
            for (int i = 0; i < visuals.Count; i++)
            {
                ExitVisual item = visuals[i];
                Vector2 offset = ExitPlanePosition(item.start, camera) - center;
                item.spread = new Vector2(offset.x * horizontalSpread, offset.y * 1.25f);
                visuals[i] = item;
            }
        }

        Vector2 ExitPlanePosition(Vector3 start, Camera camera)
        {
            if (camera != null) return new Vector2(start.x, start.y);
            Vector3 local = layout.transform.InverseTransformPoint(start);
            return new Vector2(local.x / layout.columns, local.z / (layout.rows + 2));
        }

        void ThawOtherColors(int completedColor)
        {
            foreach (Block block in blocks)
                if (block != null && block.IsFrozen && block.ColorData.ColorId != completedColor)
                    block.SetFreezeCount(block.FreezeCount - 1);
            RequestCompletionCheck();
        }

        IEnumerator PickupColor(List<Block> group, Camera camera)
        {
            var starts = new Vector3[group.Count];
            var scales = new Vector3[group.Count];
            for (int i = 0; i < group.Count; i++)
            {
                starts[i] = group[i].Visual.position;
                scales[i] = group[i].Visual.localScale;
            }
            Vector3 direction = camera != null ? -camera.transform.forward : layout.transform.up;
            float distance = PickupDistance * layout.transform.lossyScale.y;
            if (camera != null)
                foreach (Vector3 start in starts)
                    distance = Mathf.Min(distance, Mathf.Max(0,
                        camera.WorldToViewportPoint(start).z - camera.nearClipPlane) * .25f);
            float elapsed = 0;
            while (elapsed < PickupDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / PickupDuration);
                float lift = 1 - (1 - progress) * (1 - progress);
                for (int i = 0; i < group.Count; i++)
                {
                    if (group[i] == null) continue;
                    group[i].Visual.position = starts[i] + direction * (distance * lift);
                    group[i].Visual.localScale = scales[i] * Mathf.Lerp(1, PickupScale, lift);
                }
                if (progress < 1) yield return null;
            }
        }

        IEnumerator PlayColorExit(List<Block> group, Camera camera)
        {
            yield return PickupColor(group, camera);
            if (!HasRemainingGoals) FinalGoalExitStarted?.Invoke();
            List<ExitVisual> visuals = CaptureExitVisuals(group, camera);
            float elapsed = 0;
            float duration = Mathf.Max(.1f, colorExitDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                foreach (ExitVisual item in visuals) AnimateExit(item, progress, camera);
                yield return null;
            }
            foreach (Block block in group)
                if (block != null) Destroy(block.gameObject);
            removingColors--;
            ColorsRemoved?.Invoke();
        }

        void AnimateExit(ExitVisual item, float progress, Camera camera)
        {
            if (item.model == null) return;
            float pop = Mathf.Sin(progress * Mathf.PI);
            float drop = .34f * progress - 1.55f * progress * progress;
            if (camera != null)
            {
                Vector3 position = item.start;
                position.x += item.spread.x * progress;
                position.y += drop + item.spread.y * progress;
                float lift = Mathf.Min(colorExitHeight, Mathf.Max(0, item.start.z - camera.nearClipPlane) * .35f);
                position.z -= lift * pop;
                item.model.position = camera.ViewportToWorldPoint(position);
            }
            else
                item.model.position = item.start + layout.transform.up * (colorExitHeight * pop)
                    + layout.transform.right * (item.spread.x * progress * layout.columns)
                    + layout.transform.forward * ((drop + item.spread.y * progress) * (layout.rows + 2));
            Vector3 axis = camera != null ? camera.transform.forward : -layout.transform.up;
            float sign = Mathf.Sign(item.spread.x);
            item.model.rotation = Quaternion.AngleAxis(sign * progress * colorExitSpin, axis) * item.rotation;
            float scale = Mathf.Lerp(1, .78f, Mathf.Clamp01(progress * 3)) + pop * .03f;
            item.model.localScale = item.scale * scale;
        }
    }
}
