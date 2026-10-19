using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class Gameplay : UICanvas
{
    [SerializeField] GameplayResultFeedback resultFeedback;
    public GameplayResultFeedback ResultFeedback => resultFeedback;
    [SerializeField] Text levelText;
    [SerializeField] Text timerText;
    [SerializeField] Text coinText;
    [SerializeField] TimeFreezeView timeFreezeView;
    [SerializeField] HammerPowerup hammerPowerup;
    [SerializeField] UnityEvent blockBreakClicked = new UnityEvent();
    [SerializeField] UnityEvent timeFreezeClicked = new UnityEvent();
    [SerializeField] UnityEvent settingsClicked = new UnityEvent();
    public const string CoinsKey = "Coins";
    public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
    [SerializeField] RectTransform boardArea;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Image musicIcon;
    [SerializeField] Image soundIcon;
    [SerializeField] GameObject musicOffMark;
    [SerializeField] GameObject soundOffMark;
    [SerializeField] RectTransform settingsOrigin;
    [SerializeField] RectTransform[] settingsButtons;
    [SerializeField] CanvasGroup[] settingsButtonGroups;
    [SerializeField] Image settingsDimmer;
    [SerializeField, Min(.01f)] float settingsSlideDuration = .24f;
    [SerializeField, Min(0)] float settingsStagger = .035f;
    Vector2[] settingsRestPositions;
    Vector3[] settingsRestScales;
    Color settingsDimmerColor;
    float settingsProgress;
    bool settingsSessionPaused;
    float previousTimeScale = 1f;
    bool settingsOpen;
    public RectTransform BoardArea => boardArea;
    int lastSeconds = -1, lastLevel = -1;
    int lastCoins = -1;
    protected override void Update()
    {
        base.Update();
        TickSettings(Time.unscaledDeltaTime);
        int coins = Coins;
        if (coins != lastCoins && coinText != null) coinText.text = coins.ToString();
        lastCoins = coins;
        if (LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null)
            SetLevel(LevelManager.Ins.CurrentLevelNumber, LevelManager.Ins.RemainingTime);
    }
    public void SetLevel(int number, float time)
    {
        if (lastLevel != number && levelText != null) levelText.text = "Level " + number;
        int seconds = Mathf.CeilToInt(time);
        if (lastSeconds != seconds && timerText != null)
            timerText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
        lastLevel = number;
        lastSeconds = seconds;
    }
    public void OnReplay()
    {
        if (LevelManager.Ins == null) return;
        if (LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        SoundManager.Play(SoundManager.ButtonSound);
        FinishCloseSettings();
        LevelManager.Ins.OnReplay();
    }
    public void OnBlockBreak() { SoundManager.Play(SoundManager.ButtonSound); if (hammerPowerup != null) hammerPowerup.Toggle(); blockBreakClicked.Invoke(); }
    public void OnTimeFreeze()
    {
        if (timeFreezeView == null || !timeFreezeView.TryPlayIntro()) return;
        timeFreezeClicked.Invoke();
    }
    public void OnSettings()
    {
        if (hammerPowerup != null && hammerPowerup.IsStriking) return;
        if (hammerPowerup != null && hammerPowerup.IsActive) hammerPowerup.Cancel();
        if (settingsOpen) { OnCloseSettings(); return; }
        if (settingsPanel == null || !GameManager.IsState(GameState.Gameplay)) return;
        if (LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        if (!settingsSessionPaused)
        {
            CacheSettingsAnimation();
            previousTimeScale = Time.timeScale;
            settingsSessionPaused = true;
            LevelManager.Ins.SetInputEnabled(false);
            Time.timeScale = 0f;
        }
        SoundManager.Play(SoundManager.ButtonSound);
        settingsOpen = true;
        settingsPanel.SetActive(true);
        RefreshAudioButtons();
        ApplySettingsAnimation();
        settingsClicked.Invoke();
    }
    public void OnCloseSettings()
    {
        if (!settingsOpen) return;
        SoundManager.Play(SoundManager.ButtonSound);
        settingsOpen = false;
    }
    void FinishCloseSettings()
    {
        if (!settingsSessionPaused) return;
        settingsOpen = settingsSessionPaused = false;
        settingsProgress = 0f;
        for (int i = 0; i < settingsRestPositions.Length; i++)
        {
            if (settingsButtons[i] == null) continue;
            settingsButtons[i].anchoredPosition = settingsRestPositions[i];
            settingsButtons[i].localScale = settingsRestScales[i];
        }
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = previousTimeScale;
        bool loading = LoadingManager.Ins != null && LoadingManager.Ins.IsLoading;
        if (LevelManager.Ins != null)
            LevelManager.Ins.SetInputEnabled(!loading && GameManager.IsState(GameState.Gameplay));
    }
    public void OnResetData()
    {
        if (!settingsOpen || LevelManager.Ins == null) return;
        if (LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        OnCloseSettings();
        FinishCloseSettings();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        if (SoundManager.Ins != null) SoundManager.Ins.ReloadSettings();
        lastCoins = -1;
        LevelManager.Ins.OnPlay();
    }
    public override void BackKey()
    {
        if (hammerPowerup != null && hammerPowerup.IsActive) hammerPowerup.Cancel();
        else OnCloseSettings();
    }
    public void OnToggleMusic()
    {
        if (!settingsOpen || SoundManager.Ins == null) return;
        SoundManager.Ins.ToggleMusic();
        SoundManager.Play(SoundManager.ButtonSound);
        RefreshAudioButtons();
    }
    public void OnToggleSound()
    {
        if (!settingsOpen || SoundManager.Ins == null) return;
        SoundManager.Ins.ToggleSound();
        SoundManager.Play(SoundManager.ButtonSound);
        RefreshAudioButtons();
    }
    public void RefreshAudioButtons()
    {
        bool musicEnabled = SoundManager.Ins == null || SoundManager.Ins.IsMusicEnabled;
        bool soundEnabled = SoundManager.Ins == null || SoundManager.Ins.IsSoundEnabled;
        Color muted = new Color(.5f, .5f, .5f, 1f);
        if (musicIcon != null) musicIcon.color = musicEnabled ? Color.white : muted;
        if (soundIcon != null) soundIcon.color = soundEnabled ? Color.white : muted;
        if (musicOffMark != null) musicOffMark.SetActive(!musicEnabled);
        if (soundOffMark != null) soundOffMark.SetActive(!soundEnabled);
    }
    void CacheSettingsAnimation()
    {
        if (settingsRestPositions != null) return;
        Canvas.ForceUpdateCanvases();
        int count = settingsButtons != null ? settingsButtons.Length : 0;
        settingsRestPositions = new Vector2[count];
        settingsRestScales = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            if (settingsButtons[i] == null) continue;
            settingsRestPositions[i] = settingsButtons[i].anchoredPosition;
            settingsRestScales[i] = settingsButtons[i].localScale;
        }
        if (settingsDimmer != null) settingsDimmerColor = settingsDimmer.color;
    }
    void TickSettings(float deltaTime)
    {
        if (!settingsSessionPaused) return;
        float duration = Mathf.Max(.01f, settingsSlideDuration)
            + Mathf.Max(0, settingsStagger) * Mathf.Max(0, settingsRestPositions.Length - 1);
        settingsProgress = Mathf.MoveTowards(settingsProgress, settingsOpen ? 1f : 0f,
            Mathf.Max(0, deltaTime) / duration);
        ApplySettingsAnimation();
        if (!settingsOpen && settingsProgress <= 0f) FinishCloseSettings();
    }
    void ApplySettingsAnimation()
    {
        if (settingsButtons == null) return;
        float duration = Mathf.Max(.01f, settingsSlideDuration);
        float stagger = Mathf.Max(0, settingsStagger);
        float elapsed = settingsProgress * (duration + stagger * Mathf.Max(0, settingsButtons.Length - 1));
        for (int i = 0; i < settingsButtons.Length; i++)
        {
            RectTransform button = settingsButtons[i];
            if (button == null) continue;
            float progress = Mathf.Clamp01((elapsed - i * stagger) / duration);
            float ease = 1f - Mathf.Pow(1f - progress, 3f);
            button.anchoredPosition = settingsRestPositions[i];
            Vector3 rest = button.localPosition;
            Vector3 origin = settingsOrigin != null ? button.parent.InverseTransformPoint(settingsOrigin.position) : rest;
            button.localPosition = Vector3.Lerp(origin, rest, ease);
            button.localScale = settingsRestScales[i] * Mathf.Lerp(.65f, 1f, ease);
            if (settingsButtonGroups == null || i >= settingsButtonGroups.Length || settingsButtonGroups[i] == null) continue;
            settingsButtonGroups[i].alpha = progress;
            settingsButtonGroups[i].interactable = settingsOpen && progress >= 1f;
            settingsButtonGroups[i].blocksRaycasts = progress > 0f;
        }
        if (settingsDimmer == null) return;
        Color color = settingsDimmerColor;
        color.a *= settingsProgress;
        settingsDimmer.color = color;
    }
    void OnDisable() => FinishCloseSettings();
    public static void SetCoins(int amount)
    {
        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }
}
