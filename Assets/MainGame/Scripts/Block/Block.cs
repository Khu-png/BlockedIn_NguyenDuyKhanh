using System;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block : MonoBehaviour
    {
        [SerializeField] BlockColorData colorData;
        [SerializeField, HideInInspector] Vector2Int gridPosition;
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
        Quaternion visualRestRotation;
        Vector3 lastTiltPosition, tilt, tiltVelocity;
        Camera dragCamera;

        public BlockColorData ColorData => colorData;
        public Vector2Int GridPosition => gridPosition;
        public Vector2Int[] Cells => cells;
        public Collider HitCollider => hitCollider;
        public BlockAppearance Appearance => appearance;
        public bool CanDrag => movable && !exiting && !settling && !docking && !dragging && isActiveAndEnabled;
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
            visualRestRotation = visual.localRotation;
            ResetTilt();
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

        public void CopyDragSettings(Block source)
        {
            liftHeight = source.liftHeight;
            snapTolerance = source.snapTolerance;
            settleDuration = source.settleDuration;
            tiltAngle = source.tiltAngle;
            verticalTiltAngle = source.verticalTiltAngle;
            screenRollAngle = source.screenRollAngle;
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

        public bool BeginDrag(Camera camera = null)
        {
            if (board == null || !CanDrag) return false;
            dragging = true;
            dragCamera = camera;
            lastDragTarget = board.WorldToGrid(transform.position);
            lastTiltPosition = transform.position;
            visual.localPosition = visualRest + Vector3.up * liftHeight;
            return true;
        }

        public void DragTo(Vector2 target)
        {
            if (!dragging) return;
            Vector2 current = board.WorldToGrid(transform.position);
            Vector2 destination = board.ConstrainDrag(this, current, target, target - lastDragTarget);
            lastDragTarget = target;
            transform.position = board.GridToWorld(destination);
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
            if (exiting) return;
            TickTilt(deltaTime);
            if (!settling) { TickDock(deltaTime); return; }
            elapsed += deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, settleDuration));
            float smooth = progress * progress * (3 - 2 * progress);
            transform.localPosition = Vector3.Lerp(settleStart, settleTarget, smooth);
            visual.localPosition = Vector3.Lerp(visualRest + Vector3.up * releaseHeight, settleVisualTarget, smooth);
            if (progress >= 1) { settling = false; ResetTilt(); FinishSettle(); }
        }

        void TickTilt(float deltaTime)
        {
            if (board == null || visual == null || deltaTime <= 0) return;
            Vector3 movement = transform.position - lastTiltPosition;
            lastTiltPosition = transform.position;
            Vector3 speed = movement / deltaTime;
            Vector3 target = dragging ? GetTiltTarget(speed) : Vector3.zero;
            float remaining = Mathf.Min(deltaTime, .1f);
            float leanLimit = Mathf.Max(tiltAngle, verticalTiltAngle);
            float maxAngle = Mathf.Sqrt(leanLimit * leanLimit + screenRollAngle * screenRollAngle);
            while (remaining > 0)
            {
                float step = Mathf.Min(remaining, .008f);
                tiltVelocity += ((target - tilt) * 180f - tiltVelocity * 14f) * step;
                tilt += tiltVelocity * step;
                tilt = Vector3.ClampMagnitude(tilt, maxAngle);
                remaining -= step;
            }
            Quaternion rotation = tilt.sqrMagnitude > .000001f
                ? Quaternion.AngleAxis(tilt.magnitude, tilt.normalized) : Quaternion.identity;
            Quaternion parentRotation = visual.parent != null ? visual.parent.rotation : Quaternion.identity;
            visual.localRotation = Quaternion.Inverse(parentRotation) * rotation * parentRotation * visualRestRotation;
        }

        Vector3 GetTiltTarget(Vector3 speed)
        {
            Transform layout = board.Layout.transform;
            Vector3 direction = Vector3.ClampMagnitude(speed / 4f, 1);
            Vector3 forward = dragCamera != null ? dragCamera.transform.forward : -layout.up;
            Vector3 cameraRight = dragCamera != null ? dragCamera.transform.right : layout.right;
            Vector3 right = Vector3.ProjectOnPlane(cameraRight, layout.up).normalized;
            Vector3 up = Vector3.Cross(right, layout.up).normalized;
            float horizontal = Vector3.Dot(direction, right);
            float vertical = Mathf.Clamp(Vector3.Dot(speed, up) / .8f, -1, 1);
            Vector3 lean = Vector3.Cross(layout.up, right) * horizontal * tiltAngle
                + Vector3.Cross(layout.up, up) * vertical * verticalTiltAngle;
            return lean - forward * horizontal * screenRollAngle;
        }

        void ResetTilt()
        {
            tilt = tiltVelocity = Vector3.zero;
            lastTiltPosition = transform.position;
            if (visual != null) visual.localRotation = visualRestRotation;
        }

        void Update() => Tick(Time.deltaTime);

        void OnDisable()
        {
            if (board == null) return;
            dragging = settling = false;
            docking = false;
            dockPartner = null;
            ResetTilt();
            transform.position = board.GridToWorld(gridPosition);
            if (visual != null) visual.localPosition = visualRest;
            if (visual != null) visual.localScale = visualScale;
        }
    }
}
