using System.Collections;
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
    HammerTargetSelection selection;
    HammerFeedbackPool feedbackPool;
    BlockBoard board;
    Coroutine routine;
    HammerFeedback feedback;
    Block[] splitParts;
    HammerBlockImpact blockImpact;
    Coroutine retreatRoutine;
    readonly HammerCameraImpulse cameraImpulse = new HammerCameraImpulse();
    public bool IsSelecting => selection != null && selection.IsActive;
    public bool IsStriking => routine != null;
    public bool IsActive => IsSelecting || IsStriking;
    void Awake()
    {
        selection = new HammerTargetSelection(dimmedHud);
        if (feedbackPrefab != null) feedbackPool = new HammerFeedbackPool(feedbackPrefab);
    }
    bool ContextReady()
    {
        return LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null
            && GameManager.IsState(GameState.Gameplay) && Time.timeScale > 0
            && (LoadingManager.Ins == null || !LoadingManager.Ins.IsLoading);
    }
    void Update()
    {
        cameraImpulse.ClearOffset();
        if(IsActive && (!ContextReady() || LevelManager.Ins.CurrentLevel.blockGameplay != board))Cancel();
        if(button!=null)button.interactable = ContextReady() && !IsStriking && (timeFreeze==null || !timeFreeze.IsIntroducing);
    }
    void LateUpdate() => cameraImpulse.Tick(Time.deltaTime);
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
        if (selection == null) selection = new HammerTargetSelection(dimmedHud);
        if (!selection.Begin(owner)) return false;
        if (feedbackPool == null) feedbackPool = new HammerFeedbackPool(feedbackPrefab);
        board = owner;
        board.PowerupBusy = true;
        board.PowerupPress = Select;
        return true;
    }
    public bool Select(Block target)
    {
        if(!IsSelecting || IsStriking)return true;
        if(!selection.CanSelect(target))return true;
        ClearSelection();
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
        feedback=feedbackPool.Rent();
        yield return feedback.Fly(start,center,rotation,.7f*board.Layout.transform.lossyScale.x);
        yield return feedback.Swing(center,rotation);
        if(target==null||board==null||!target.CanSplit){Finish();yield break;}
        feedback.Hit(center, rotation, Mathf.Clamp(Mathf.Sqrt(target.Cells.Length) * .8f, 1.35f, 2.4f)
            * board.Layout.transform.lossyScale.x);
        cameraImpulse.Play(camera);
        try { splitParts=board.SplitBlock(target); }
        catch(System.Exception error) { Debug.LogException(error); }
        if(splitParts==null){Finish();yield break;}
        blockImpact = new HammerBlockImpact(splitParts, board);
        retreatRoutine=StartCoroutine(feedback.Retreat(rotation));
        yield return blockImpact.Play();
        if(retreatRoutine!=null)yield return retreatRoutine;
        retreatRoutine=null;
        Finish();
    }
    void ClearSelection() => selection?.Clear();
    public void Cancel()
    {
        if(routine!=null)StopCoroutine(routine);
        if(retreatRoutine!=null)StopCoroutine(retreatRoutine);retreatRoutine=null;
        ClearSelection();Finish();
    }
    void Finish()
    {
        cameraImpulse.Restore();
        blockImpact?.Restore();
        if(feedback!=null)feedbackPool.Return(feedback);
        if(board!=null)
        {
            if(board.PowerupPress==Select)board.PowerupPress=null;
            board.PowerupBusy=false;board.RequestCompletionCheck();
            if(splitParts!=null)foreach(Block block in splitParts)if(block!=null)block.RefreshDockPorts();
        }
        routine=null;feedback=null;board=null;
        splitParts=null;blockImpact=null;
    }
    void OnDisable()=>Cancel();
    void OnDestroy() => feedbackPool?.Dispose();
}
