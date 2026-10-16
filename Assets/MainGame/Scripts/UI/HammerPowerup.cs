using System.Collections;
using System.Collections.Generic;
using BlockedIn.Blocks;
using UnityEngine;
using UnityEngine.UI;
public sealed class HammerPowerup : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] RectTransform launchOrigin;
    [SerializeField] HammerFeedback feedbackPrefab;
    [SerializeField] TimeFreezeView timeFreeze;
    [SerializeField] Graphic[] dimmedHud = System.Array.Empty<Graphic>();
    static readonly int SelectionMode = Shader.PropertyToID("_HammerMode");
    readonly HashSet<Block> eligible = new HashSet<Block>();
    Color[] hudColors = System.Array.Empty<Color>();
    BlockBoard board;
    Coroutine routine;
    HammerFeedback feedback;
    Block[] splitParts;
    HammerBlockImpact blockImpact;
    public bool IsSelecting { get; private set; }
    public bool IsStriking => routine != null;
    public bool IsActive => IsSelecting || IsStriking;
    void Awake()
    {
        hudColors = new Color[dimmedHud.Length];
        for(int i=0;i<dimmedHud.Length;i++)if(dimmedHud[i]!=null)hudColors[i]=dimmedHud[i].color;
    }
    bool ContextReady()
    {
        return LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null
            && GameManager.IsState(GameState.Gameplay) && Time.timeScale > 0
            && (LoadingManager.Ins == null || !LoadingManager.Ins.IsLoading);
    }
    void Update()
    {
        if(IsActive && (!ContextReady() || LevelManager.Ins.CurrentLevel.blockGameplay != board))Cancel();
        if(button!=null)button.interactable = ContextReady() && !IsStriking && (timeFreeze==null || !timeFreeze.IsIntroducing);
    }
    public void Toggle()
    {
        if(IsStriking)return;
        if(IsSelecting){Cancel();return;}
        if(!ContextReady() || feedbackPrefab==null || (timeFreeze!=null&&timeFreeze.IsIntroducing))return;
        TryBegin(LevelManager.Ins.CurrentLevel.blockGameplay);
    }
    public bool TryBegin(BlockBoard owner)
    {
        if(IsActive || owner==null || owner.PowerupBusy || feedbackPrefab==null)return false;
        foreach(Block block in owner.Blocks)if(block!=null&&(block.IsDragging||block.IsSettling))return false;
        eligible.Clear();
        foreach(Block block in owner.Blocks)
            if(block!=null&&block.CanSplit)eligible.Add(block);
        if(eligible.Count==0)return false;
        board=owner;board.PowerupBusy=true;board.PowerupPress=Select;
        IsSelecting=true;Shader.SetGlobalFloat(SelectionMode,1);
        foreach(Block block in board.Blocks)if(block!=null)block.ShowHammerSelection(true,eligible.Contains(block));
        SetHudDimmed(true);
        return true;
    }
    public bool Select(Block target)
    {
        if(!IsSelecting || IsStriking)return true;
        if(target==null || !eligible.Contains(target) || !target.CanSplit)return true;
        IsSelecting=false;ClearSelection();
        routine=StartCoroutine(Strike(target));
        return true;
    }
    IEnumerator Strike(Block target)
    {
        Camera camera=board.ViewCamera;
        Quaternion rotation=camera!=null?camera.transform.rotation:Quaternion.LookRotation(-board.Layout.transform.up,board.Layout.transform.forward);
        Vector3 center=Vector3.zero;
        foreach(Vector2Int cell in target.Cells)center+=board.GridToWorld(target.GridPosition+cell);
        center/=target.Cells.Length;
        center+=board.Layout.transform.up*.9f;
        Vector3 start=center+rotation*new Vector3(-3,-3,0);
        if(camera!=null&&launchOrigin!=null)
        {
            Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,launchOrigin.position);
            var plane=new Plane(board.Layout.transform.up,center);
            Ray ray=camera.ScreenPointToRay(screen);
            if(plane.Raycast(ray,out float distance))start=ray.GetPoint(distance);
        }
        feedback=Instantiate(feedbackPrefab);
        yield return feedback.Fly(start,center,rotation,.7f*board.Layout.transform.lossyScale.x);
        yield return feedback.Swing(center,rotation);
        if(target==null||board==null||!target.CanSplit){Finish();yield break;}
        feedback.Hit(center);
        try { splitParts=board.SplitBlock(target); }
        catch(System.Exception error) { Debug.LogException(error); }
        if(splitParts==null){Finish();yield break;}
        blockImpact = new HammerBlockImpact(splitParts, board);
        yield return blockImpact.Play();
        yield return feedback.Retreat(rotation);
        Finish();
    }
    void ClearSelection()
    {
        Shader.SetGlobalFloat(SelectionMode,0);
        if(board!=null)foreach(Block block in board.Blocks)if(block!=null)block.ShowHammerSelection(false,false);
        SetHudDimmed(false);
    }
    void SetHudDimmed(bool dimmed)
    {
        for(int i=0;i<dimmedHud.Length;i++)if(dimmedHud[i]!=null)
        {
            Color color=hudColors[i];if(dimmed){color.r*=.3f;color.g*=.3f;color.b*=.3f;}dimmedHud[i].color=color;
        }
    }
    public void Cancel()
    {
        if(routine!=null)StopCoroutine(routine);
        ClearSelection();Finish();
    }
    void Finish()
    {
        blockImpact?.Restore();
        if(feedback!=null)Destroy(feedback.gameObject);
        if(board!=null)
        {
            if(board.PowerupPress==Select)board.PowerupPress=null;
            board.PowerupBusy=false;board.RequestCompletionCheck();
            if(splitParts!=null)foreach(Block block in splitParts)if(block!=null)block.RefreshDockPorts();
        }
        IsSelecting=false;routine=null;feedback=null;board=null;
        splitParts=null;blockImpact=null;eligible.Clear();
    }
    void OnDisable()=>Cancel();
}
