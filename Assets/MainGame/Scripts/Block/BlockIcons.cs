using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField, HideInInspector] SpriteRenderer[] iconRenderers = System.Array.Empty<SpriteRenderer>();
        public SpriteRenderer[] IconRenderers => iconRenderers;
        [SerializeField] Material iconMaterial;
        const float IconWidth = .48f;
        const float IconHeight = .398f;

        public void ApplyIcons()
        {
            if (visual == null || cells == null || cells.Length == 0) return;
            if (colorData == null || colorData.Icon == null) { RefreshIcons(); return; }
            if (iconRenderers == null || iconRenderers.Length != cells.Length)
            {
                ClearIcons();
                iconRenderers = new SpriteRenderer[cells.Length];
            }
            for (int i = 0; i < cells.Length; i++)
                if (iconRenderers[i] == null) iconRenderers[i] = CreateIcon(i);
            RefreshIcons();
        }
        SpriteRenderer CreateIcon(int index)
        {
            bool separateModels = ExitVisualCount == cells.Length;
            Transform parent = separateModels ? GetExitVisual(index) : visual;
            var go = new GameObject("ColorIcon");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            Vector3 point = separateModels ? Vector3.up * IconHeight
                : new Vector3(cells[index].x, IconHeight, -cells[index].y);
            go.transform.localPosition = Quaternion.Inverse(parent.localRotation) * point;
            go.transform.localRotation = Quaternion.Inverse(parent.localRotation) * Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = -1;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
        void RefreshIcons()
        {
            if (iconRenderers == null) return;
            Sprite sprite = colorData != null ? colorData.Icon : null;
            foreach (SpriteRenderer renderer in iconRenderers)
            {
                if (renderer == null) continue;
                renderer.sprite = sprite;
                if (iconMaterial != null)
                {
                    renderer.sharedMaterial = iconMaterial;
                    renderer.sortingOrder = 0;
                }
                renderer.color = colorData != null ? colorData.IconTint : Color.white;
                renderer.enabled = sprite != null;
                if (sprite == null) continue;
                float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                renderer.transform.localScale = Vector3.one * (IconWidth / Mathf.Max(.001f, size));
            }
        }
        void ClearIcons()
        {
            if (iconRenderers == null) return;
            foreach (SpriteRenderer renderer in iconRenderers)
            {
                if (renderer == null) continue;
                if (Application.isPlaying) Destroy(renderer.gameObject);
                else DestroyImmediate(renderer.gameObject);
            }
        }
    }
}
