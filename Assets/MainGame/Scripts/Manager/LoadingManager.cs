using System;
using System.Collections;
using UnityEngine;

public class LoadingManager : Singleton<LoadingManager>
{
    [SerializeField] LoadingScreen loadingPrefab;
    [SerializeField] Transform canvasParent;
    [SerializeField, Min(.1f)] float progressSpeed = .9f;
    LoadingScreen screen;
    Coroutine loadingRoutine;
    public bool IsLoading { get; private set; }
    public float Progress { get; private set; }

    void Awake() => RegisterSingleton(this);

    public void LoadGameplay(int levelIndex)
    {
        if (IsLoading) return;
        if (loadingPrefab == null || canvasParent == null)
            throw new InvalidOperationException("Assign the loading prefab and its canvas parent.");
        if (screen == null) screen = Instantiate(loadingPrefab, canvasParent);
        IsLoading = true;
        LevelManager.Ins.SetInputEnabled(false);
        screen.Show();
        SetProgress(0);
        loadingRoutine = StartCoroutine(LoadLevel(levelIndex));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        // Render the cover before creating the next board.
        yield return null;
        yield return MoveProgress(.2f);
        if (!PrepareGameplay(levelIndex, false)) yield break;
        yield return MoveProgress(.6f);
        if (!PrepareGameplay(levelIndex, true)) yield break;
        yield return MoveProgress(.9f);
        // Allow block initialization, layout and camera fitting to finish.
        yield return null;
        yield return null;
        yield return MoveProgress(1);
        LevelManager.Ins.PrepareBoardIntro();
        float elapsed = 0;
        while (elapsed < .12f)
        {
            elapsed += Time.unscaledDeltaTime;
            screen.SetAlpha(1 - Mathf.Clamp01(elapsed / .12f));
            yield return null;
        }
        screen.Hide();
        elapsed = 0;
        while (elapsed < .28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / .28f);
            LevelManager.Ins.SetBoardIntroProgress(1 - Mathf.Pow(1 - progress, 3));
            yield return null;
        }
        LevelManager.Ins.SetBoardIntroProgress(1);
        Finish(true);
    }

    bool PrepareGameplay(int levelIndex, bool openUI)
    {
        try
        {
            if (openUI) LevelManager.Ins.OnInit();
            else LevelManager.Ins.OnLoadLevel(levelIndex);
            LevelManager.Ins.SetInputEnabled(false);
            return true;
        }
        catch (Exception error)
        {
            Finish(false);
            Debug.LogException(error);
            return false;
        }
    }

    IEnumerator MoveProgress(float target)
    {
        while (Progress < target)
        {
            SetProgress(Mathf.MoveTowards(Progress, target, progressSpeed * Time.unscaledDeltaTime));
            yield return null;
        }
    }

    void SetProgress(float value)
    {
        Progress = Mathf.Clamp01(value);
        screen.SetProgress(Progress);
    }

    void Finish(bool ready)
    {
        screen.Hide();
        IsLoading = false;
        loadingRoutine = null;
        if (ready) LevelManager.Ins.OnLoadingComplete();
    }

    void OnDisable()
    {
        if (loadingRoutine != null) StopCoroutine(loadingRoutine);
        if (IsLoading && LevelManager.Ins != null) LevelManager.Ins.SetBoardIntroProgress(1);
        loadingRoutine = null;
        IsLoading = false;
        if (screen != null) screen.Hide();
    }
}
