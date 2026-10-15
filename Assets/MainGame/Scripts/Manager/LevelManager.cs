using System;
using UnityEngine;
using UnityEngine.EventSystems;
using BlockedIn.MapTools;
using BlockedIn.CameraTools;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#endif

[ExecuteAlways]
public class LevelManager : Singleton<LevelManager>
{
    public const string CurrentLevelKey = "CurrentLevel";
    public static int SavedLevelNumber => Mathf.Max(1, PlayerPrefs.GetInt(CurrentLevelKey, 1));
    [SerializeField] LevelData[] levels = Array.Empty<LevelData>();
    [SerializeField] CameraManager cameraManager;
    [SerializeField] EventSystem uiEvents;
    [SerializeField] GeneratedBoard editorPreview;
    [SerializeField, Min(1f)] float timeFreezeDuration = 30f;
    GeneratedBoard currentLevel;
    int currentLevelIndex;
    float remainingTime;
    float remainingTimeFreeze;
    bool hasGoals;
    public LevelData[] Levels => levels;
    public int CurrentLevelNumber => levels[currentLevelIndex].levelNumber;
    public float RemainingTime => remainingTime;
    public float RemainingTimeFreeze => remainingTimeFreeze;
    public float TimeFreezeProgress => Mathf.Clamp01(remainingTimeFreeze / Mathf.Max(1f, timeFreezeDuration));
    public bool CanFreezeTime => currentLevel != null && remainingTime > 0f && remainingTimeFreeze <= 0f
        && GameManager.IsState(GameState.Gameplay) && Time.timeScale > 0f
        && (LoadingManager.Ins == null || !LoadingManager.Ins.IsLoading);
    public GeneratedBoard CurrentLevel => currentLevel;
    public bool HasNextLevel => HasLevelData(currentLevelIndex + 1);

    void Awake()
    {
        if (Application.isPlaying) RegisterSingleton(this);
    }
    public void OnPlay()
    {
        int index = Array.FindIndex(levels, level => level != null && level.levelNumber == SavedLevelNumber);
        StartLevel(index >= 0 ? index : 0);
    }
    public void OnInit()
    {
        GameManager.Ins.OnPlay();
        Gameplay view = UIManager.Ins.GetUI<Gameplay>();
        if (view != null && view.BoardArea != null) cameraManager.Configure(cameraManager.MainCamera, view.BoardArea);
        cameraManager.FocusLevel(currentLevel);
    }

    public void OnLoadLevel(int index)
    {
        if (!HasLevelData(index)) throw new InvalidOperationException("Assign a valid LevelData SO.");
        LevelData data = levels[index];
        if (data.RuntimeBoard == null) throw new InvalidOperationException("Save this level in Map Generator first.");
        if (cameraManager == null || cameraManager.MainCamera == null)
            throw new InvalidOperationException("Assign CameraManager and its Main Camera.");
        OnDespawn();
        if (editorPreview != null) editorPreview.gameObject.SetActive(false);
        currentLevelIndex = index;
        remainingTime = Mathf.Max(1, data.timeLimit);
        currentLevel = Instantiate(data.RuntimeBoard, transform);
        if (currentLevel.blockGameplay != null)
        {
            currentLevel.blockGameplay.Configure(currentLevel, cameraManager);
            hasGoals = currentLevel.blockGameplay.HasRemainingGoals;
            currentLevel.blockGameplay.ColorsRemoved += CheckWin;
        }
        else hasGoals = false;
        if (currentLevel.Input != null)
            currentLevel.Input.Configure(cameraManager.MainCamera, currentLevel.blockGameplay, uiEvents);
        cameraManager.FocusLevel(currentLevel);
    }
    void Update()
    {
        if (!Application.isPlaying || currentLevel == null || !GameManager.IsState(GameState.Gameplay)) return;
        if (LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        if (currentLevel.blockGameplay != null && currentLevel.blockGameplay.IsRemovingColors
            && !currentLevel.blockGameplay.HasRemainingGoals) return;
        float delta = Time.deltaTime;
        float frozenDelta = Mathf.Min(remainingTimeFreeze, delta);
        remainingTimeFreeze -= frozenDelta;
        remainingTime = Mathf.Max(0, remainingTime - (delta - frozenDelta));
        if (remainingTime <= 0) GameManager.Ins.OnLose();
    }
    public bool TryFreezeTime()
    {
        if (!CanFreezeTime) return false;
        remainingTimeFreeze = Mathf.Max(1f, timeFreezeDuration);
        return true;
    }
    void CheckWin()
    {
        if (LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        if (hasGoals && currentLevel != null && !currentLevel.blockGameplay.IsRemovingColors
            && !currentLevel.blockGameplay.HasRemainingGoals) OnWin();
    }
    public void OnWin()
    {
        if (!GameManager.IsState(GameState.Gameplay)) return;
        int next = HasNextLevel ? currentLevelIndex + 1 : currentLevelIndex;
        PlayerPrefs.SetInt(CurrentLevelKey, levels[next].levelNumber);
        PlayerPrefs.Save();
        GameManager.Ins.OnFinish();
    }
    public void OnReplay()
    {
        StartLevel(currentLevelIndex);
    }
    public void OnNextLevel()
    {
        if (!GameManager.IsState(GameState.Win) || !HasNextLevel) return;
        StartLevel(currentLevelIndex + 1);
    }
    void StartLevel(int index)
    {
        if (LoadingManager.Ins != null) LoadingManager.Ins.LoadGameplay(index);
        else LoadAndPlay(index);
    }
    void LoadAndPlay(int index)
    {
        OnLoadLevel(index);
        OnInit();
    }
    public void OnLoadingComplete()
    {
        SetInputEnabled(GameManager.IsState(GameState.Gameplay));
        GameManager.Ins.StartGameplayMusic();
        CheckWin();
    }
    public void PrepareBoardIntro() => cameraManager.PrepareLevelIntro();
    public void SetBoardIntroProgress(float value) => cameraManager.SetLevelIntroProgress(value);

    public void ResetSavedLevel()
    {
        if (Application.isPlaying && LoadingManager.Ins != null && LoadingManager.Ins.IsLoading) return;
        PlayerPrefs.SetInt(CurrentLevelKey, 1);
        PlayerPrefs.Save();
        if (Application.isPlaying) OnPlay();
    }
    public void OnRestart() => OnReplay();
    public void SetInputEnabled(bool value)
    {
        if (currentLevel != null && currentLevel.Input != null) currentLevel.Input.enabled = value;
    }
    public void OnDespawn()
    {
        remainingTimeFreeze = 0f;
        if (currentLevel == null) return;
        if (currentLevel.blockGameplay != null) currentLevel.blockGameplay.ColorsRemoved -= CheckWin;
        currentLevel.gameObject.SetActive(false);
        Destroy(currentLevel.gameObject);
        currentLevel = null;
    }
    bool HasLevelData(int index) => index >= 0 && index < levels.Length && levels[index] != null;

#if UNITY_EDITOR
    static readonly HashSet<LevelManager> registered = new();
    void OnEnable()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid()) return;
        registered.Add(this);
        RefreshLevels();
    }
    void OnDisable() => registered.Remove(this);
    public static void RefreshRegisteredLevels()
    {
        foreach (LevelManager manager in registered)
            if (manager != null) manager.RefreshLevels();
    }
    public void RefreshLevels()
    {
        const string folder = "Assets/MainGame/ScriptableObject/Levels";
        if (Application.isPlaying || !Directory.Exists(folder)) return;
        var result = new List<LevelData>();
        foreach (LevelData level in levels)
            if (level != null && !result.Contains(level)) result.Add(level);
        foreach (string path in Directory.GetFiles(folder, "*.asset"))
        {
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path.Replace('\\', '/'));
            if (level != null && !result.Contains(level)) result.Add(level);
        }
        result.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
        bool changed = result.Count != levels.Length;
        for (int i = 0; !changed && i < result.Count; i++) changed = result[i] != levels[i];
        if (!changed) return;
        Undo.RecordObject(this, "Update level list");
        levels = result.ToArray();
        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
