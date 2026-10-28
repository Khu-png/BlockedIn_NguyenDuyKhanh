using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        [SerializeField, Min(.01f)] float dragFollowTime = .045f;
        [SerializeField, Min(1f)] float dragFollowSpeed = 35f;
        Vector2 dragFollowVelocity;

        public void DragTo(Vector2 target)
        {
            if (!dragging) return;
            FollowDragTarget(target, Time.deltaTime);
            board.CaptureFloorShadowTrail(this);
            board.ShowPlacementPreview(this, snapTolerance);
        }

        void FollowDragTarget(Vector2 target, float deltaTime)
        {
            float step = Mathf.Clamp(deltaTime, 0, 1f / 30f);
            if (step <= 0) return;
            Vector2 current = board.WorldToGrid(transform.position);
            Vector2 reachable = board.ConstrainDrag(this, current, target, target - lastDragTarget, step);
            if (Vector2.Dot(reachable - current, dragFollowVelocity) < 0) dragFollowVelocity = Vector2.zero;
            Vector2 next = Vector2.SmoothDamp(current, reachable, ref dragFollowVelocity,
                dragFollowTime, dragFollowSpeed, step);
            next = current + Vector2.ClampMagnitude(next - current, dragFollowSpeed * step);
            Vector2 destination = board.ConstrainDragStep(this, current, next, reachable, step);
            if (Mathf.Abs(destination.x - next.x) > BlockBoard.PositionEpsilon) dragFollowVelocity.x = 0;
            if (Mathf.Abs(destination.y - next.y) > BlockBoard.PositionEpsilon) dragFollowVelocity.y = 0;
            lastDragTarget = target;
            transform.position = board.GridToWorld(destination);
        }
    }
}
