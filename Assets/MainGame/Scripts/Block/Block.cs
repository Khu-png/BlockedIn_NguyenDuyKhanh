using System;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block : MonoBehaviour
    {
        [SerializeField] BlockColorData colorData;
        [SerializeField] Vector2Int gridPosition;
        [SerializeField] Vector2Int[] cells = { Vector2Int.zero };
        [SerializeField] Transform visual;
        [Tooltip("All visual renderers, including inactive Tab, Socket and corner variants.")]
        [SerializeField] Renderer[] renderers;
        [SerializeField] Collider hitCollider;
        [SerializeField] BlockAppearance appearance;
        [SerializeField] bool movable = true;
        [SerializeField, Min(0)] float liftHeight = .15f;
        [SerializeField, Range(0, .49f)] float snapTolerance = .2f;
        [SerializeField, Min(.01f)] float settleDuration = .12f;
        BlockBoard board;
        bool dragging, settling;
        float elapsed;
        Vector3 settleStart, settleTarget, visualRest;
        float releaseHeight;

        public BlockColorData ColorData => colorData;
        public Vector2Int GridPosition => gridPosition;
        public Vector2Int[] Cells => cells;
        public Collider HitCollider => hitCollider;
        public BlockAppearance Appearance => appearance;
        public bool CanDrag => movable && !settling && !docking && !dragging && isActiveAndEnabled;
        public bool IsDragging => dragging;
        public bool IsSettling => settling || docking;

        public void Configure(BlockColorData color, Vector2Int position, Transform model,
            Renderer[] modelRenderers, Collider collider, bool canMove = true)
        {
            colorData = color;
            gridPosition = position;
            visual = model;
            renderers = modelRenderers;
            hitCollider = collider;
            movable = canMove;
            ApplyColor();
            if (appearance != null) appearance.Apply();
        }

        public void Initialize(BlockBoard owner)
        {
            if (visual == null || hitCollider == null || cells == null || cells.Length == 0)
                throw new InvalidOperationException("Assign a visual, collider and footprint to each block.");
            board = owner;
            visualRest = visual.localPosition;
            visualScale = visual.localScale;
            transform.position = owner.GridToWorld(gridPosition);
            ApplyColor();
            if (appearance != null) appearance.Apply();
        }

        public void SetColor(BlockColorData color)
        {
            colorData = color;
            ApplyColor();
            if (board != null) board.RequestCompletionCheck();
        }

        public bool HasSameColor(Block other)
        {
            return other != null && colorData != null && other.colorData != null
                && colorData.ColorId == other.colorData.ColorId;
        }

        public void ApplyColor()
        {
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

        public bool BeginDrag()
        {
            if (board == null || !CanDrag) return false;
            dragging = true;
            visual.localPosition = visualRest + Vector3.up * liftHeight;
            return true;
        }

        public void DragTo(Vector2 target)
        {
            if (!dragging) return;
            Vector2 current = board.WorldToGrid(transform.position);
            transform.position = board.GridToWorld(board.Constrain(this, current, target));
        }

        public void EndDrag()
        {
            if (!dragging) return;
            Vector2 current = board.WorldToGrid(transform.position);
            if (!board.TrySnap(this, current, snapTolerance, out Vector2Int destination))
                throw new InvalidOperationException("No reachable grid position for the dragged block.");
            dragging = false;
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
            if (!settling) { TickDock(deltaTime); return; }
            elapsed += deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, settleDuration));
            float smooth = progress * progress * (3 - 2 * progress);
            transform.localPosition = Vector3.Lerp(settleStart, settleTarget, smooth);
            visual.localPosition = Vector3.Lerp(visualRest + Vector3.up * releaseHeight, settleVisualTarget, smooth);
            if (progress >= 1) { settling = false; FinishSettle(); }
        }

        void Update() => Tick(Time.deltaTime);

        void OnDisable()
        {
            if (board == null) return;
            dragging = settling = false;
            docking = false;
            dockPartner = null;
            transform.position = board.GridToWorld(gridPosition);
            if (visual != null) visual.localPosition = visualRest;
            if (visual != null) visual.localScale = visualScale;
        }
    }
}
