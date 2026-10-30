using UnityEngine;

public sealed class Win : UICanvas
{
    [SerializeField] ResultPopupSequence reveal;
    [SerializeField] ResultPopupEffects effects;
    [SerializeField] WinIntroEffect intro;
    public bool IsReady => (intro==null || intro.IsPanelShown) && (reveal==null || reveal.IsReady);
    [SerializeField] UnityEngine.UI.Button nextButton;
    [SerializeField] UnityEngine.UI.Text levelText;
    [SerializeField] UnityEngine.UI.Text messageText;
    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        if(reveal!=null)reveal.Sample(0,true);
        if(intro!=null)intro.Play(ShowPanel);
        else ShowPanel();
        bool hasNext = LevelManager.Ins != null && LevelManager.Ins.HasNextLevel;
        if (nextButton != null) nextButton.interactable = hasNext;
        if (levelText != null && LevelManager.Ins != null)
            levelText.text = "Level " + LevelManager.Ins.CurrentLevelNumber;
        if (messageText != null) messageText.text = hasNext
            ? "Ready for the next puzzle?" : "You've completed every level!";
    }
    void ShowPanel()
    {
        reveal?.Stop();
        effects?.Play(true);
        if(reveal!=null)reveal.Play(true);
    }
    public void OnReplay() { if(!IsReady)return; SoundManager.Play(SoundManager.ButtonSound); LevelManager.Ins.OnReplay(); }
    public void OnNextLevel()
    {
        if (!IsReady) return;
        SoundManager.Play(SoundManager.ButtonSound);
        if (LoadingManager.Ins != null)
        {
            LoadingManager.Ins.PlayWinTransition(GameManager.Ins.OnNextLevel);
            return;
        }
        GameManager.Ins.OnNextLevel();
    }
}
