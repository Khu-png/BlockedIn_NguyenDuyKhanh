using System.Collections;
using UnityEngine;

public enum GameState { Gameplay, Win, Lose }

public class GameManager : Singleton<GameManager>
{
    static GameState gameState;
    Coroutine resultRoutine;
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
        CancelResult();
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
        BeginResult(true);
    }
    public void OnLose()
    {
        if (!IsState(GameState.Gameplay)) return;
        ChangeState(GameState.Lose);
        LevelManager.Ins.SetInputEnabled(false);
        BeginResult(false);
    }
    void BeginResult(bool win)
    {
        if(SoundManager.Ins!=null)SoundManager.Ins.SetMusicSuspended(true);
        if(!win)SoundManager.Play(SoundManager.LoseSound);
        resultRoutine=StartCoroutine(ShowResult(win));
    }
    IEnumerator ShowResult(bool win)
    {
        Gameplay view=UIManager.Ins.GetUI<Gameplay>();
        if(view!=null&&view.ResultFeedback!=null)yield return view.ResultFeedback.Play(win);
        else yield return new WaitForSecondsRealtime(win?.1f:.32f);
        if(win&&LevelManager.Ins!=null&&LevelManager.Ins.CurrentLevel!=null)
            yield return LevelManager.Ins.CurrentLevel.PlayWinResult();
        if(!IsState(win?GameState.Win:GameState.Lose)){resultRoutine=null;yield break;}
        if(LoadingManager.Ins!=null&&LoadingManager.Ins.IsLoading){resultRoutine=null;yield break;}
        if(win){UIManager.Ins.OpenUI<Win>();SoundManager.Play(SoundManager.WinSound);}
        else UIManager.Ins.OpenUI<Lose>();
        resultRoutine=null;
    }
    void CancelResult()
    {
        if(resultRoutine!=null)StopCoroutine(resultRoutine);resultRoutine=null;
        if(LevelManager.Ins!=null&&LevelManager.Ins.CurrentLevel!=null)
            LevelManager.Ins.CurrentLevel.RestoreWinResult();
        if(UIManager.Ins==null||!UIManager.Ins.IsLoaded<Gameplay>())return;
        Gameplay view=UIManager.Ins.GetUI<Gameplay>();
        if(view!=null&&view.ResultFeedback!=null)view.ResultFeedback.Restore();
    }
    void OnDisable()=>CancelResult();
    public void OnReplay() => LevelManager.Ins.OnReplay();
    public void OnNextLevel()
    {
        if (IsState(GameState.Win)) LevelManager.Ins.OnNextLevel();
    }
}
