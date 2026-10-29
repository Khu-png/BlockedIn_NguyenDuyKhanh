using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class WinIntroEffect : MonoBehaviour
{
    [SerializeField] RectTransform host;
    [SerializeField] Sprite logoSprite;
    RectTransform root, logo;
    CanvasGroup group;
    Coroutine routine;
    public bool IsPanelShown { get; private set; }

    public void Play(Action showPanel)
    {
        Stop();
        EnsureVisuals();
        if (root == null) { IsPanelShown = true; showPanel?.Invoke(); return; }
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        routine = StartCoroutine(Animate(showPanel));
    }

    void EnsureVisuals()
    {
        if (root != null || host == null || logoSprite == null) return;
        var rootObject = new GameObject("Win Logo Intro");
        root = rootObject.AddComponent<RectTransform>();
        root.SetParent(host, false);
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.sizeDelta = new Vector2(950, 950);
        rootObject.AddComponent<CanvasRenderer>();
        ResultGlowGraphic glow = rootObject.AddComponent<ResultGlowGraphic>();
        glow.color = new Color(.18f, .9f, 1, .6f);
        group = rootObject.AddComponent<CanvasGroup>();

        var logoObject = new GameObject("Blocked In Logo");
        logo = logoObject.AddComponent<RectTransform>();
        logo.SetParent(root, false);
        logo.anchorMin = logo.anchorMax = new Vector2(.5f, .5f);
        logo.sizeDelta = new Vector2(760, 400);
        logoObject.AddComponent<CanvasRenderer>();
        Image image = logoObject.AddComponent<Image>();
        image.sprite = logoSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    IEnumerator Animate(Action showPanel)
    {
        IsPanelShown = false;
        bool panelShown = false;
        float elapsed = 0;
        while (elapsed < .86f)
        {
            elapsed += Time.unscaledDeltaTime;
            float enter = Ease(Mathf.Clamp01(elapsed / .24f));
            float lift = Smooth(Mathf.Clamp01((elapsed - .38f) / .28f));
            group.alpha = Mathf.Min(enter, 1 - Smooth(Mathf.Clamp01((elapsed - .56f) / .22f)));
            root.anchoredPosition = Vector2.up * Mathf.Lerp(40, 650, lift);
            root.localScale = Vector3.one * Mathf.Lerp(.5f + enter * .5f
                + Mathf.Sin(enter * Mathf.PI) * .1f, .52f, lift);
            if (!panelShown && elapsed >= .5f)
            {
                panelShown = IsPanelShown = true;
                showPanel?.Invoke();
            }
            yield return null;
        }
        root.gameObject.SetActive(false);
        routine = null;
    }

    static float Ease(float value) => 1 - Mathf.Pow(1 - value, 3);
    static float Smooth(float value) => value * value * (3 - 2 * value);

    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsPanelShown = false;
        if (root != null) root.gameObject.SetActive(false);
    }

    void OnDisable() => Stop();
}
