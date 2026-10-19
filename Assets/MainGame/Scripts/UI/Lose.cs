using UnityEngine;

public sealed class Lose : UICanvas
{
    [SerializeField] ResultPopupSequence reveal;
    public bool IsReady => reveal==null || reveal.IsReady;
    [SerializeField] UnityEngine.UI.Text levelText;
    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        if(reveal!=null)reveal.Play(false);
        if (levelText != null && LevelManager.Ins != null)
            levelText.text = "Level " + LevelManager.Ins.CurrentLevelNumber;
    }
    public void OnReplay() { if(!IsReady)return; SoundManager.Play(SoundManager.ButtonSound); LevelManager.Ins.OnReplay(); }
}
