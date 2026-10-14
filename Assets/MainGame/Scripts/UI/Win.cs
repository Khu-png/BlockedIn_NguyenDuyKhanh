using UnityEngine;

public sealed class Win : UICanvas
{
    [SerializeField] UnityEngine.UI.Button nextButton;
    [SerializeField] UnityEngine.UI.Text levelText;
    [SerializeField] UnityEngine.UI.Text messageText;
    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        bool hasNext = LevelManager.Ins != null && LevelManager.Ins.HasNextLevel;
        if (nextButton != null) nextButton.interactable = hasNext;
        if (levelText != null && LevelManager.Ins != null)
            levelText.text = "Level " + LevelManager.Ins.CurrentLevelNumber;
        if (messageText != null) messageText.text = hasNext
            ? "Ready for the next puzzle?" : "You've completed every level!";
    }
    public void OnReplay() { SoundManager.Play(SoundManager.ButtonSound); LevelManager.Ins.OnReplay(); }
    public void OnNextLevel() { SoundManager.Play(SoundManager.ButtonSound); GameManager.Ins.OnNextLevel(); }
}
