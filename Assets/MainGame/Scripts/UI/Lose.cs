using UnityEngine;

public sealed class Lose : UICanvas
{
    [SerializeField] UnityEngine.UI.Text levelText;
    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        if (levelText != null && LevelManager.Ins != null)
            levelText.text = "Level " + LevelManager.Ins.CurrentLevelNumber;
    }
    public void OnReplay() => LevelManager.Ins.OnReplay();
}
