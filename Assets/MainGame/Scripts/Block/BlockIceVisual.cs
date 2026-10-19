using System.Collections;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockIceVisual : MonoBehaviour
    {
        [SerializeField] MeshFilter shell;
        [SerializeField] MeshRenderer shellRenderer;
        [SerializeField] TextMesh countText;
        [SerializeField, Min(.01f)] float shakeDuration = .2f;
        [SerializeField, Min(0)] float shakeDistance = .055f;
        [SerializeField, Min(0)] float shakeAngle = 4f;
        [SerializeField] Material shardMaterial;
        [SerializeField] Mesh[] shardMeshes;
        BlockIceShards shards;
        TextMesh counterShadow;
        Vector3 restPosition, restScale;
        Quaternion restRotation;
        Coroutine animationRoutine;
        int shownCount = -1;
        public MeshFilter Shell => shell;
        public TextMesh CountText => countText;
        public bool HasShardAssets => shardMaterial != null && shardMeshes != null && shardMeshes.Length > 0;
        public void ConfigureShardAssets(Material material, Mesh[] meshes)
        {
            if (shardMaterial == null) shardMaterial = material;
            if (shardMeshes == null || shardMeshes.Length == 0) shardMeshes = meshes;
        }

        public void Configure(MeshFilter model, MeshRenderer renderer, TextMesh label)
        {
            shell = model;
            shellRenderer = renderer;
            countText = label;
            CaptureRestPose();
        }

        void Awake()
        {
            CaptureRestPose();
            CreateCounterShadow();
            if (!HasShardAssets || shell == null || !Application.isPlaying) return;
            var root = new GameObject("Ice Shards");
            root.transform.SetParent(transform.parent, false);
            root.layer = gameObject.layer;
            shards = root.AddComponent<BlockIceShards>();
            shards.Configure(shell.transform, shell.sharedMesh, shardMeshes, shardMaterial);
        }

        void CreateCounterShadow()
        {
            if (!Application.isPlaying || countText == null || countText.font == null) return;
            var root = new GameObject("Ice Counter Shadow");
            root.layer = countText.gameObject.layer;
            root.transform.SetParent(countText.transform, false);
            root.transform.localPosition = new Vector3(.014f, -.016f, .003f);
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            counterShadow = root.AddComponent<TextMesh>();
            counterShadow.font = countText.font;
            counterShadow.fontSize = countText.fontSize;
            counterShadow.characterSize = countText.characterSize;
            counterShadow.anchor = countText.anchor;
            counterShadow.alignment = countText.alignment;
            counterShadow.text = countText.text;
            counterShadow.color = new Color(.04f, .16f, .3f, .9f);
            renderer.sharedMaterial = countText.font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void CaptureRestPose()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
        }

        public void ShowCount(int count)
        {
            if (count == shownCount) return;
            bool reduced = shownCount >= 0 && shownCount > count;
            if (reduced && Application.isPlaying && shards != null) shards.Emit(count == 0);
            shownCount = count;
            StopAnimation();
            if (countText != null)
            {
                countText.text = count.ToString();
                if (counterShadow != null) counterShadow.text = countText.text;
                countText.gameObject.SetActive(count > 0);
            }
            if (count > 0)
            {
                gameObject.SetActive(true);
                if (reduced && Application.isPlaying && gameObject.activeInHierarchy)
                    animationRoutine = StartCoroutine(ShakeIce());
            }
            else if (Application.isPlaying && gameObject.activeInHierarchy)
                animationRoutine = StartCoroutine(BreakIce());
            else gameObject.SetActive(false);
        }

        void RestorePose()
        {
            transform.localPosition = restPosition;
            transform.localRotation = restRotation;
            transform.localScale = restScale;
            if (shellRenderer != null) shellRenderer.SetPropertyBlock(null);
        }

        void StopAnimation()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            RestorePose();
        }

        IEnumerator ShakeIce()
        {
            float elapsed = 0;
            float duration = Mathf.Max(.01f, shakeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI * 8) * (1 - progress);
                transform.localPosition = restPosition + Vector3.right * (wave * shakeDistance);
                transform.localRotation = restRotation * Quaternion.AngleAxis(wave * shakeAngle, Vector3.up);
                yield return null;
            }
            animationRoutine = null;
            RestorePose();
        }

        IEnumerator BreakIce()
        {
            float elapsed = 0;
            var properties = new MaterialPropertyBlock();
            Color tint = shellRenderer != null && shellRenderer.sharedMaterial != null
                ? shellRenderer.sharedMaterial.GetColor("_BaseColor") : new Color(.68f, .91f, 1, .5f);
            while (elapsed < .18f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / .18f);
                transform.localScale = restScale * (1 + .12f * progress);
                Color fadingTint = tint;fadingTint.a *= 1 - progress;
                properties.SetColor("_BaseColor", fadingTint);
                if (shellRenderer != null) shellRenderer.SetPropertyBlock(properties);
                yield return null;
            }
            animationRoutine = null;
            RestorePose();
            gameObject.SetActive(false);
        }

        void OnDisable() => StopAnimation();
        void OnDestroy()
        {
            if (shards != null) Destroy(shards.gameObject);
        }
    }

    [System.Serializable]
    public struct BlockIceData
    {
        public int colorId;
        [Min(1)] public int count;
    }
}
