using UnityEngine;

public sealed class Lose : UICanvas
{
    public void OnReplay() => LevelManager.Ins.OnReplay();
}
