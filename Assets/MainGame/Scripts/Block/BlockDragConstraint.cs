using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        const float LaneEntryTolerance = .38f;
        const float PointerLaneTolerance = .38f;
        const float LaneReleaseTolerance = .48f;
        const float LaneAlignSpeed = 14f;
        const float DirectionPriority = 1.2f;
        Block guidedBlock;
        bool horizontalLane;
        float activeLane;

        public void ResetDragGuidance(Block block)
        {
            if (guidedBlock == block) guidedBlock = null;
        }

        public Vector2 ConstrainDrag(Block block, Vector2 current, Vector2 target, Vector2 pointerDelta,
            float deltaTime = 1f / 60f)
        {
            Vector2 result = BestSlide(block, current, target);
            if (guidedBlock == block)
            {
                float cross = horizontalLane ? target.y : target.x;
                Vector2 aligned = horizontalLane ? new Vector2(current.x, activeLane)
                    : new Vector2(activeLane, current.y);
                Vector2 destination = horizontalLane ? new Vector2(target.x, activeLane)
                    : new Vector2(activeLane, target.y);
                destination = Slide(block, aligned, destination, horizontalLane);
                if (Mathf.Abs(cross - activeLane) <= LaneReleaseTolerance
                    && (LaneRestricted(block, aligned, horizontalLane)
                        || LaneRestricted(block, destination, horizontalLane)))
                    return FollowLane(block, current, target, deltaTime);
                guidedBlock = null;
            }
            Vector2 intent = target - current;
            if (intent.sqrMagnitude < PositionEpsilon * PositionEpsilon) intent = pointerDelta;
            if (Mathf.Abs(intent.x) > Mathf.Abs(intent.y) * DirectionPriority)
                return GuideLane(block, current, target, result, true, deltaTime);
            if (Mathf.Abs(intent.y) > Mathf.Abs(intent.x) * DirectionPriority)
                return GuideLane(block, current, target, result, false, deltaTime);
            return result;
        }

        bool LaneRestricted(Block block, Vector2 position, bool horizontal)
        {
            float cross = horizontal ? position.y : position.x;
            return Mathf.Abs(LimitAxis(block, position, cross + .04f, !horizontal) - cross) < .02f
                || Mathf.Abs(LimitAxis(block, position, cross - .04f, !horizontal) - cross) < .02f;
        }

        Vector2 FollowLane(Block block, Vector2 current, Vector2 target, float deltaTime)
        {
            float cross = horizontalLane ? current.y : current.x;
            float next = Mathf.MoveTowards(cross, activeLane, LaneAlignSpeed * Mathf.Max(0, deltaTime));
            next = LimitAxis(block, current, next, !horizontalLane);
            if (horizontalLane) { current.y = next; target.y = next; }
            else { current.x = next; target.x = next; }
            return Slide(block, current, target, horizontalLane);
        }

        Vector2 BestSlide(Block block, Vector2 current, Vector2 target)
        {
            Vector2 horizontal = Slide(block, current, target, true);
            Vector2 vertical = Slide(block, current, target, false);
            return (horizontal - target).sqrMagnitude <= (vertical - target).sqrMagnitude ? horizontal : vertical;
        }

        Vector2 Slide(Block block, Vector2 current, Vector2 target, bool horizontalFirst)
        {
            if (horizontalFirst)
            {
                current.x = LimitAxis(block, current, target.x, true);
                current.y = LimitAxis(block, current, target.y, false);
            }
            else
            {
                current.y = LimitAxis(block, current, target.y, false);
                current.x = LimitAxis(block, current, target.x, true);
            }
            return current;
        }

        Vector2 GuideLane(Block block, Vector2 current, Vector2 target, Vector2 normal, bool horizontal,
            float deltaTime)
        {
            float cross = horizontal ? current.y : current.x;
            float pointerCross = horizontal ? target.y : target.x;
            float lane = Mathf.Round(cross);
            if (Mathf.Abs(cross - lane) > LaneEntryTolerance
                || Mathf.Abs(pointerCross - lane) > PointerLaneTolerance) return normal;
            float reached = LimitAxis(block, current, lane, !horizontal);
            if (Mathf.Abs(reached - lane) > PositionEpsilon) return normal;
            Vector2 aligned = current;
            Vector2 guidedTarget = target;
            if (horizontal) { aligned.y = lane; guidedTarget.y = lane; }
            else { aligned.x = lane; guidedTarget.x = lane; }
            Vector2 guided = Slide(block, aligned, guidedTarget, horizontal);
            float direction = Mathf.Sign(horizontal ? target.x - current.x : target.y - current.y);
            float gain = direction * (horizontal ? guided.x - normal.x : guided.y - normal.y);
            // Assist only when alignment improves forward travel; open areas keep free dragging.
            if (gain <= PositionEpsilon) return normal;
            guidedBlock = block;
            horizontalLane = horizontal;
            activeLane = lane;
            return FollowLane(block, current, target, deltaTime);
        }
    }
}
