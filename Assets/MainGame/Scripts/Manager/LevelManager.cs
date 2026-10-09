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
    GeneratedBoard currentLevel;
    int currentLevelIndex;
    float remainingTime;
    bool hasGoals;
    public LevelData[] Levels => levels;
    public int CurrentLevelNumber => levels[currentLevelIndex].levelNumber;
    public float RemainingTime => remainingTime;
    public GeneratedBoard CurrentLevel => currentLevel;
    public bool HasNextLevel => HasLevelData(currentLevelIndex + 1);

    void Awake()
    {
        if (Application.isPlaying) RegisterSingleton(this);
    }
    public void OnPlay()
    {
        int index = Array.FindIndex(levels, level => level != null && level.levelNumber == SavedLevelNumber);
        OnLoadLevel(index >= 0 ? index : 0);
        OnInit();
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
        if (currentLevel.blockGameplay != null && currentLevel.blockGameplay.IsRemovingColors
            && !currentLevel.blockGameplay.HasRemainingGoals) return;
        remainingTime = Mathf.Max(0, remainingTime - Time.deltaTime);
        if (remainingTime <= 0) GameManager.Ins.OnLose();
    }
    void CheckWin()
    {
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
        OnLoadLevel(currentLevelIndex);
        OnInit();
    }
    public void OnNextLevel()
    {
        if (!GameManager.IsState(GameState.Win) || !HasNextLevel) return;
        OnLoadLevel(currentLevelIndex + 1);
        OnInit();
    }
    public void OnRestart() => OnReplay();
    public void SetInputEnabled(bool value)
    {
        if (currentLevel != null && currentLevel.Input != null) currentLevel.Input.enabled = value;
    }
    public void OnDespawn()
    {
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
