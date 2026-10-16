using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class TimeFreezeView : MonoBehaviour
{
    [SerializeField] CanvasGroup frostGroup;
    [SerializeField] CanvasGroup timerFrostGroup;
    [SerializeField] CanvasGroup freezeBarGroup;
    [SerializeField] Image freezeBarFill;
    [SerializeField] TimeFreezeFeedback feedback;
    [SerializeField] Image timerPanel;
    [SerializeField] Button freezeButton;
    [SerializeField] RectTransform introIcon;
    [SerializeField] RectTransform introOrigin;
    [SerializeField] CanvasGroup introGroup;
    [SerializeField, Min(.1f)] float introDuration = 1.25f;
    [SerializeField] Color frozenTimerColor = new Color(.1f, .45f, .75f, 1f);
    Color normalTimerColor;
    Coroutine introRoutine;
    float lastBarProgress = -1f;
    public bool IsIntroducing { get; private set; }

    void Awake()
    {
        if (timerPanel != null) normalTimerColor = timerPanel.color;
        ClearVisuals();
    }
    void Update()
    {
        LevelManager manager = LevelManager.Ins;
        bool loading = LoadingManager.Ins != null && LoadingManager.Ins.IsLoading;
        bool playing = manager != null && manager.CurrentLevel != null
            && GameManager.IsState(GameState.Gameplay) && !loading;
        float seconds = playing ? manager.RemainingTimeFreeze : 0f;
        bool frozen = seconds > 0f;
        bool hammerBusy = playing && manager.CurrentLevel.blockGameplay != null && manager.CurrentLevel.blockGameplay.PowerupBusy;
        if (freezeButton != null) freezeButton.interactable = playing && !hammerBusy && !frozen && !IsIntroducing && Time.timeScale > 0f;
        if (frostGroup != null)
            frostGroup.alpha = Mathf.MoveTowards(frostGroup.alpha, frozen ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        if (timerFrostGroup != null)
            timerFrostGroup.alpha = frostGroup != null ? frostGroup.alpha : (frozen ? 1f : 0f);
        if (freezeBarGroup != null)
            freezeBarGroup.alpha = frostGroup != null ? frostGroup.alpha : (frozen ? 1f : 0f);
        SetBarProgress(playing ? manager.TimeFreezeProgress : 0f);
        if (timerPanel != null) timerPanel.color = frozen ? frozenTimerColor : normalTimerColor;
    }
    public bool TryPlayIntro()
    {
        LevelManager manager = LevelManager.Ins;
        if (IsIntroducing || manager == null || !manager.CanFreezeTime || introIcon == null || introGroup == null) return false;
        if (manager.CurrentLevel.blockGameplay != null && manager.CurrentLevel.blockGameplay.PowerupBusy) return false;
        IsIntroducing = true;
        if (freezeButton != null) freezeButton.interactable = false;
        if (feedback != null) feedback.BeginFlight();
        SoundManager.Play("TimeFreeze");
        introRoutine = StartCoroutine(PlayIntro(manager));
        return true;
    }
    IEnumerator PlayIntro(LevelManager manager)
    {
        var board = manager.CurrentLevel;
        Vector3 start = introOrigin != null ? introIcon.parent.InverseTransformPoint(introOrigin.position) : Vector3.down * 500f;
        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            bool loading = LoadingManager.Ins != null && LoadingManager.Ins.IsLoading;
            if (manager.CurrentLevel != board || loading || !GameManager.IsState(GameState.Gameplay))
            { FinishIntro(); yield break; }
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / introDuration);
            float flight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .65f));
            Vector3 bend = start * .5f + Vector3.right * 100f;
            introIcon.localPosition = (1f - flight) * (1f - flight) * start + 2f * (1f - flight) * flight * bend;
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.4f, 1f, progress));
            introIcon.localScale = Vector3.one * Mathf.Lerp(.85f, 3.2f, grow);
            introIcon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(progress * Mathf.PI) * -12f);
            introGroup.alpha = Mathf.Clamp01(progress / .08f) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.82f, 1f, progress)));
            if (feedback != null) feedback.SampleFlight(introIcon.localPosition, progress, Time.deltaTime);
            yield return null;
        }
        FinishIntro();
        if (manager.CurrentLevel == board) manager.TryFreezeTime();
    }
    void FinishIntro()
    {
        IsIntroducing = false;
        introRoutine = null;
        if (introGroup != null) introGroup.alpha = 0f;
        if (feedback != null) feedback.EndFlight();
    }
    void OnDisable()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        FinishIntro();
        ClearVisuals();
    }
    void ClearVisuals()
    {
        if (frostGroup != null) frostGroup.alpha = 0f;
        if (timerFrostGroup != null) timerFrostGroup.alpha = 0f;
        if (freezeBarGroup != null) freezeBarGroup.alpha = 0f;
        SetBarProgress(0f);
        if (introGroup != null) introGroup.alpha = 0f;
        if (timerPanel != null) timerPanel.color = normalTimerColor;
        if (feedback != null) feedback.Clear();
    }
    void SetBarProgress(float progress)
    {
        if (Mathf.Approximately(lastBarProgress, progress)) return;
        lastBarProgress = progress;
        if (feedback != null) feedback.SetBarProgress(progress);
        if (freezeBarFill == null) return;
        freezeBarFill.fillAmount = progress;
        freezeBarFill.enabled = progress > 0f;
        freezeBarFill.rectTransform.anchorMax = new Vector2(progress, 1f);
    }
}
