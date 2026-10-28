using UnityEngine;

public sealed class ResultPopupEffects : MonoBehaviour
{
    [SerializeField] RectTransform hero;
    [SerializeField] RectTransform halo;
    [SerializeField] RectTransform primaryButton;
    [SerializeField] RectTransform confettiRoot;
    ResultConfetti confetti;
    Vector2 heroPosition;
    Vector3 heroScale, buttonScale;
    Quaternion heroRotation;
    float elapsed;
    bool cached, playing, victory, heroMotionStarted;

    public void Play(bool win)
    {
        Cache(); Restore();
        victory = win; elapsed = 0; playing = true; heroMotionStarted = false;
        if (win && confettiRoot != null)
        {
            confettiRoot.SetAsLastSibling();
            confetti ??= new ResultConfetti(confettiRoot);
        }
        confetti?.Sample(-1);
    }

    void Cache()
    {
        if (cached) return;
        cached = true;
        if (hero != null)
        {
            heroPosition = hero.anchoredPosition;
            heroScale = hero.localScale;
            heroRotation = hero.localRotation;
        }
        if (primaryButton != null) buttonScale = primaryButton.localScale;
    }

    void LateUpdate()
    {
        if (!playing) return;
        elapsed += Time.unscaledDeltaTime;
        if (victory) confetti?.Sample(elapsed);
        if (halo != null) halo.localRotation = Quaternion.Euler(0,0,elapsed * (victory ? 9 : -4));
        if (hero != null)
        {
            if (!victory && elapsed < .85f)
                hero.localRotation = heroRotation * Quaternion.Euler(0,0,
                    Mathf.Sin(elapsed * 31) * 11 * Mathf.Clamp01(1 - elapsed / .85f));
            else if (victory && elapsed > .85f)
            {
                if (!heroMotionStarted)
                {
                    heroPosition = hero.anchoredPosition;
                    heroRotation = hero.localRotation;
                    heroMotionStarted = true;
                }
                float floatTime = elapsed - .85f;
                float blend = Mathf.SmoothStep(0, 1, floatTime / .35f);
                hero.localRotation = heroRotation;
                hero.anchoredPosition = heroPosition
                    + Vector2.up * (Mathf.Sin(floatTime * 2.2f) * 6 * blend);
            }
            else if (!victory && elapsed >= .85f) hero.localRotation = heroRotation;
        }
        if (elapsed > 1 && primaryButton != null)
            primaryButton.localScale = buttonScale * (1 + Mathf.Sin((elapsed-1)*3)*.012f);
    }

    void Restore()
    {
        if (!cached) return;
        if (hero != null)
        {
            hero.anchoredPosition = heroPosition;
            hero.localRotation = heroRotation;
            hero.localScale = heroScale;
        }
        if (primaryButton != null) primaryButton.localScale = buttonScale;
        if (halo != null) halo.localRotation = Quaternion.identity;
        confetti?.Sample(-1);
    }

    void OnDisable() { playing = false; Restore(); }
}
