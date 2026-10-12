using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class Gameplay : UICanvas
{
    [SerializeField] Text levelText;
    [SerializeField] Text timerText;
    [SerializeField] Text coinText;
    [SerializeField] UnityEvent blockBreakClicked = new UnityEvent();
    [SerializeField] UnityEvent timeFreezeClicked = new UnityEvent();
    [SerializeField] UnityEvent settingsClicked = new UnityEvent();
    public const string CoinsKey = "Coins";
    public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
    [SerializeField] RectTransform boardArea;
    [SerializeField] GameObject settingsPanel;
    float previousTimeScale = 1f;
    bool settingsOpen;
    public RectTransform BoardArea => boardArea;
    int lastSeconds = -1, lastLevel = -1;
    int lastCoins = -1;
    protected override void Update()
    {
        base.Update();
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
    public void OnReplay() => LevelManager.Ins.OnReplay();
    public void OnBlockBreak() => blockBreakClicked.Invoke();
    public void OnTimeFreeze() => timeFreezeClicked.Invoke();
    public void OnSettings()
    {
        if (settingsPanel == null || settingsOpen || !GameManager.IsState(GameState.Gameplay)) return;
        if (LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        previousTimeScale = Time.timeScale;
        settingsOpen = true;
        settingsPanel.SetActive(true);
        LevelManager.Ins.SetInputEnabled(false);
        Time.timeScale = 0f;
        settingsClicked.Invoke();
    }
    public void OnCloseSettings()
    {
        if (!settingsOpen) return;
        settingsOpen = false;
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
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        lastCoins = -1;
        LevelManager.Ins.OnPlay();
    }
    public override void BackKey() => OnCloseSettings();
    void OnDisable() => OnCloseSettings();
    public static void SetCoins(int amount)
    {
        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }
}
