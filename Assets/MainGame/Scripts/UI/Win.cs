using UnityEngine;

public sealed class Win : UICanvas
{
    [SerializeField] ResultPopupSequence reveal;
    public bool IsReady => reveal==null || reveal.IsReady;
    [SerializeField] UnityEngine.UI.Button nextButton;
    [SerializeField] UnityEngine.UI.Text levelText;
    [SerializeField] UnityEngine.UI.Text messageText;
    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        if(reveal!=null)reveal.Play(true);
        bool hasNext = LevelManager.Ins != null && LevelManager.Ins.HasNextLevel;
        if (nextButton != null) nextButton.interactable = hasNext;
        if (levelText != null && LevelManager.Ins != null)
            levelText.text = "Level " + LevelManager.Ins.CurrentLevelNumber;
        if (messageText != null) messageText.text = hasNext
            ? "Ready for the next puzzle?" : "You've completed every level!";
    }
    public void OnReplay() { if(!IsReady)return; SoundManager.Play(SoundManager.ButtonSound); LevelManager.Ins.OnReplay(); }
    public void OnNextLevel() { if(!IsReady)return; SoundManager.Play(SoundManager.ButtonSound); GameManager.Ins.OnNextLevel(); }
}
