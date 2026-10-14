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
        if (LoadingManager.Ins == null || !LoadingManager.Ins.IsLoading) StartGameplayMusic();
    }
    public void StartGameplayMusic()
    {
        if (!IsState(GameState.Gameplay)) return;
        if (SoundManager.Ins != null)
        {
            SoundManager.Ins.SetMusicSuspended(false);
            SoundManager.Ins.PlayMusic(SoundManager.GameplayMusic);
        }
    }
    public void OnFinish()
    {
        if (!IsState(GameState.Gameplay)) return;
        ChangeState(GameState.Win);
        LevelManager.Ins.SetInputEnabled(false);
        UIManager.Ins.OpenUI<Win>();
        if (SoundManager.Ins != null) SoundManager.Ins.SetMusicSuspended(true);
        SoundManager.Play(SoundManager.WinSound);
    }
    public void OnLose()
    {
        if (!IsState(GameState.Gameplay)) return;
        ChangeState(GameState.Lose);
        LevelManager.Ins.SetInputEnabled(false);
        UIManager.Ins.OpenUI<Lose>();
        if (SoundManager.Ins != null) SoundManager.Ins.SetMusicSuspended(true);
        SoundManager.Play(SoundManager.LoseSound);
    }
    public void OnReplay() => LevelManager.Ins.OnReplay();
    public void OnNextLevel()
    {
        if (IsState(GameState.Win)) LevelManager.Ins.OnNextLevel();
    }
}
