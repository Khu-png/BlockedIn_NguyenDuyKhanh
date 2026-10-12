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
        Vector3 restPosition, restScale;
        Quaternion restRotation;
        Coroutine animationRoutine;
        int shownCount = -1;
        public MeshFilter Shell => shell;
        public TextMesh CountText => countText;

        public void Configure(MeshFilter model, MeshRenderer renderer, TextMesh label)
        {
            shell = model;
            shellRenderer = renderer;
            countText = label;
            CaptureRestPose();
        }

        void Awake() => CaptureRestPose();

        void CaptureRestPose()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
        }

        public void ShowCount(int count)
        {
            if (count == shownCount) return;
            bool reduced = shownCount > count && count > 0;
            shownCount = count;
            StopAnimation();
            if (countText != null)
            {
                countText.text = count.ToString();
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
            while (elapsed < .18f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / .18f);
                transform.localScale = restScale * (1 + .12f * progress);
                properties.SetColor("_BaseColor", new Color(.8f, .95f, 1, .42f * (1 - progress)));
                if (shellRenderer != null) shellRenderer.SetPropertyBlock(properties);
                yield return null;
            }
            animationRoutine = null;
            RestorePose();
            gameObject.SetActive(false);
        }

        void OnDisable() => StopAnimation();
    }

    [System.Serializable]
    public struct BlockIceData
    {
        public int colorId;
        [Min(1)] public int count;
    }
}
