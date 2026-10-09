using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        [SerializeField, Min(.1f)] float colorExitDuration = 1.05f;
        [SerializeField, Min(0)] float colorExitHeight = 1.5f;
        [SerializeField, Min(0)] float colorExitSpin = 60f;
        int removingColors;
        public bool IsRemovingColors => removingColors > 0;

        struct ExitVisual
        {
            public Transform model;
            public Vector3 start, scale;
            public Quaternion rotation;
            public float spread, rise;
        }

        void RemoveColor(List<Block> group)
        {
            var visuals = new List<ExitVisual>();
            Camera camera = ViewCamera;
            foreach (Block block in group)
            {
                blocks.Remove(block);
                block.BeginExit();
                for (int i = 0; i < block.ExitVisualCount; i++)
                {
                    Transform model = block.GetExitVisual(i);
                    int index = visuals.Count;
                    Vector3 start = camera != null ? camera.WorldToViewportPoint(model.position) : model.position;
                    visuals.Add(new ExitVisual { model = model, start = start, scale = model.localScale,
                        rotation = model.rotation, spread = (index % 2 == 0 ? -1 : 1) * (.035f + index % 3 * .008f),
                        rise = .3f + index % 3 * .04f });
                }
            }
            removingColors++;
            StartCoroutine(PlayColorExit(group, visuals, camera));
        }

        IEnumerator PlayColorExit(List<Block> group, List<ExitVisual> visuals, Camera camera)
        {
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
            float drop = item.rise * progress - 1.55f * progress * progress;
            if (camera != null)
            {
                Vector3 position = item.start;
                position.x += item.spread * progress;
                position.y += drop;
                float lift = Mathf.Min(colorExitHeight, Mathf.Max(0, item.start.z - camera.nearClipPlane) * .35f);
                position.z -= lift * pop;
                item.model.position = camera.ViewportToWorldPoint(position);
            }
            else
                item.model.position = item.start + layout.transform.up * (colorExitHeight * pop)
                    + layout.transform.right * (item.spread * progress * layout.columns)
                    + layout.transform.forward * (drop * (layout.rows + 2));
            Vector3 axis = camera != null ? camera.transform.forward : -layout.transform.up;
            float sign = Mathf.Sign(item.spread);
            item.model.rotation = Quaternion.AngleAxis(sign * progress * colorExitSpin, axis) * item.rotation;
            float scale = camera == null || camera.orthographic ? 1 + pop * .12f : 1;
            item.model.localScale = item.scale * scale;
        }
    }
}
