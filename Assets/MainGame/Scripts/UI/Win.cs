using UnityEngine;

public sealed class Win : UICanvas
{
    [SerializeField] UnityEngine.UI.Button nextButton;
    public override void Open()
    {
        base.Open();
        if (nextButton != null) nextButton.interactable = LevelManager.Ins.HasNextLevel;
    }
    public void OnReplay() => LevelManager.Ins.OnReplay();
    public void OnNextLevel() => GameManager.Ins.OnNextLevel();
}
