using UnityEngine;

public enum GameState { Gameplay, Win, Lose }

public class GameManager : Singleton<GameManager>
{
    static GameState gameState;
    void Awake()
    {
        RegisterSingleton(this);
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    void Start() => OnInit();
    public static bool IsState(GameState state) => gameState == state;
    public static void ChangeState(GameState state) => gameState = state;
    public void OnInit() => LevelManager.Ins.OnPlay();

    public void OnPlay()
    {
        ChangeState(GameState.Gameplay);
        UIManager.Ins.CloseAll();
        UIManager.Ins.OpenUI<Gameplay>();
    }
    public void OnFinish()
    {
        if (!IsState(GameState.Gameplay)) return;
        ChangeState(GameState.Win);
        LevelManager.Ins.SetInputEnabled(false);
        UIManager.Ins.OpenUI<Win>();
    }
    public void OnLose()
    {
        if (!IsState(GameState.Gameplay)) return;
        ChangeState(GameState.Lose);
        LevelManager.Ins.SetInputEnabled(false);
        UIManager.Ins.OpenUI<Lose>();
    }
    public void OnReplay() => LevelManager.Ins.OnReplay();
    public void OnNextLevel()
    {
        if (IsState(GameState.Win)) LevelManager.Ins.OnNextLevel();
    }
}
