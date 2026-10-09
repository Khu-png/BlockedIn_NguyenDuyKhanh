using UnityEngine;
using UnityEngine.UI;

public sealed class Gameplay : UICanvas
{
    [SerializeField] Text levelText;
    [SerializeField] Text timerText;
    [SerializeField] RectTransform boardArea;
    public RectTransform BoardArea => boardArea;
    int lastSeconds = -1, lastLevel = -1;
    protected override void Update()
    {
        base.Update();
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
}
