using System;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block : MonoBehaviour
    {
        [SerializeField] BlockColorData colorData;
        [SerializeField, HideInInspector] Vector2Int gridPosition;
        [SerializeField, HideInInspector] Vector2Int goalPosition = new Vector2Int(-1, -1);
        [SerializeField, HideInInspector] Vector2Int[] cells = { Vector2Int.zero };
        [SerializeField] Transform visual;
        [SerializeField] GameObject iceVisual;
        [SerializeField] TextMesh iceCountText;
        public GameObject IceVisual => iceVisual;
        public TextMesh IceCountText => iceCountText;
        [Tooltip("All visual renderers, including inactive Tab, Socket and corner variants.")]
        [SerializeField] Renderer[] renderers;
        [SerializeField] Collider hitCollider;
        [SerializeField] BlockAppearance appearance;
        [SerializeField, HideInInspector] MeshFilter[] hitMeshSources;
        public MeshFilter[] HitMeshSources => hitMeshSources;
        [SerializeField] bool movable = true;
        [SerializeField, Min(0)] float liftHeight = .15f;
        [SerializeField, Range(0, .49f)] float snapTolerance = .2f;
        [SerializeField, Min(.01f)] float settleDuration = .12f;
        [SerializeField, Range(0, 12)] float tiltAngle = 10f;
        [SerializeField, Range(0, 90)] float verticalTiltAngle = 90f;
        [SerializeField, Range(0, 12)] float screenRollAngle = 8f;
        BlockBoard board;
        bool dragging, settling;
        float elapsed;
        Vector3 settleStart, settleTarget, visualRest;
        float releaseHeight;
        Vector2 lastDragTarget;
        BlockTiltMotion tiltMotion;

        public BlockColorData ColorData => colorData;
        public Vector2Int GridPosition => gridPosition;
        public Vector2Int GoalPosition => goalPosition;
        public bool HasGoalPosition => goalPosition.x >= 0 && goalPosition.y >= 0;
        public Vector2Int[] Cells => cells;
        public Collider HitCollider => hitCollider;
        public BlockAppearance Appearance => appearance;
        public bool CanDrag => movable && !IsFrozen && !IsRoped && !exiting && !settling && !docking && !dragging && isActiveAndEnabled;
        public bool IsDragging => dragging;
        public bool IsSettling => settling || docking;

        public void Configure(BlockColorData color, Vector2Int position, Transform model,
            Renderer[] modelRenderers, Collider collider, bool canMove = true)
        {
            colorData = color;
            gridPosition = position;
            goalPosition = position;
            visual = model;
            renderers = modelRenderers;
            hitCollider = collider;
            movable = canMove;
            ApplyColor();
            if (appearance != null) appearance.Apply();
        }

        public void SetGoalPosition(Vector2Int position) => goalPosition = position;
        public void Initialize(BlockBoard owner)
        {
            if (visual == null || hitCollider == null || cells == null || cells.Length == 0)
                throw new InvalidOperationException("Assign a visual, collider and footprint to each block.");
            board = owner;
            visualRest = visual.localPosition;
            visualScale = visual.localScale;
            transform.position = owner.GridToWorld(gridPosition);
            tiltMotion = new BlockTiltMotion(transform, visual, owner.Layout.transform,
                tiltAngle, verticalTiltAngle, screenRollAngle);
            SetFreezeCount(freezeCount);
            ApplyColor();
            if (appearance != null) appearance.Apply();
            ApplyIcons();
        }

        public void SetColor(BlockColorData color)
        {
            colorData = color;
            ApplyColor();
            ApplyIcons();
            if (board != null) board.RequestCompletionCheck();
        }

        public void SetTransitionLayer(int layer)
        {
            gameObject.layer = layer;
            foreach (Renderer item in renderers)
                if (item != null) item.gameObject.layer = layer;
        }

        public void CopyDragSettings(Block source)
        {
            liftHeight = source.liftHeight;
            snapTolerance = source.snapTolerance;
            settleDuration = source.settleDuration;
            tiltAngle = source.tiltAngle;
            verticalTiltAngle = source.verticalTiltAngle;
            screenRollAngle = source.screenRollAngle;
            tiltMotion?.Configure(tiltAngle, verticalTiltAngle, screenRollAngle);
            dragFollowTime = source.dragFollowTime;
            dragFollowSpeed = source.dragFollowSpeed;
        }

        public bool HasSameColor(Block other)
        {
            return other != null && colorData != null && other.colorData != null
                && colorData.ColorId == other.colorData.ColorId;
        }

        public void ApplyColor()
        {
            RefreshIcons();
            RefreshTabLayer();
            if (colorData == null || colorData.Material == null || renderers == null) return;
            foreach (Renderer item in renderers)
            {
                if (item == null) continue;
                Material[] materials = item.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = colorData.Material;
                item.sharedMaterials = materials;
            }
        }

        void OnValidate() => ApplyColor();
        void OnEnable() => ApplyColor();

        public bool BeginDrag(Camera camera = null)
        {
            if (board == null || board.PowerupBusy || !CanDrag) return false;
            dragging = true;
            dragFollowVelocity = Vector2.zero;
            board.BeginFloorShadowTrail(this);
            board.RefreshDockNeighborhood(this);
            board.ResetDragGuidance(this);
            lastDragTarget = board.WorldToGrid(transform.position);
            tiltMotion?.Begin(camera);
            visual.localPosition = visualRest + Vector3.up * liftHeight;
            board.ShowPlacementPreview(this, snapTolerance);
            return true;
        }

        public void EndDrag()
        {
            if (!dragging) return;
            board.ResetDragGuidance(this);
            Vector2 current = board.WorldToGrid(transform.position);
            if (!board.TrySnap(this, current, snapTolerance, out Vector2Int destination))
                destination = gridPosition;
            dragging = false;
            SoundManager.Play(SoundManager.DropSound);
            board.HidePlacementPreview(this);
            gridPosition = destination;
            PrepareDock();
            settleStart = transform.localPosition;
            settleTarget = transform.parent.InverseTransformPoint(board.GridToWorld(destination));
            releaseHeight = visual.localPosition.y - visualRest.y;
            elapsed = 0;
            settling = true;
            board.RequestCompletionCheck();
        }

        public void Tick(float deltaTime)
        {
            if (exiting) return;
            tiltMotion?.Tick(dragging, deltaTime);
            if (!settling) { TickDock(deltaTime); return; }
            elapsed += deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, settleDuration));
            float smooth = progress * progress * (3 - 2 * progress);
            transform.localPosition = Vector3.Lerp(settleStart, settleTarget, smooth);
            visual.localPosition = Vector3.Lerp(visualRest + Vector3.up * releaseHeight, settleVisualTarget, smooth);
            if (progress >= 1) { settling = false; ResetTilt(); FinishSettle(); }
        }

        void ResetTilt() => tiltMotion?.Reset();

        void Update()
        {
            if (!dragging && !settling && !docking && !matchPulseActive) return;
            Tick(Time.deltaTime);
        }
        void OnDisable()
        {
            if (board == null) return;
            board.ResetDragGuidance(this);
            board.HidePlacementPreview(this);
            dragging = settling = matchPulseActive = false;
            docking = false;
            dockPartner = null;
            RefreshDockPorts();
            ResetTilt();
            transform.position = board.GridToWorld(gridPosition);
            if (visual != null) visual.localPosition = visualRest;
            if (visual != null) visual.localScale = visualScale;
        }
    }
}
