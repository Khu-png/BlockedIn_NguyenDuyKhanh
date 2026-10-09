using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        const float LaneEntryTolerance = .2f;
        const float PointerLaneTolerance = .3f;
        const float DirectionPriority = 1.2f;

        public Vector2 ConstrainDrag(Block block, Vector2 current, Vector2 target, Vector2 pointerDelta)
        {
            Vector2 result = BestSlide(block, current, target);
            if (Mathf.Abs(pointerDelta.x) > Mathf.Abs(pointerDelta.y) * DirectionPriority)
                return GuideLane(block, current, target, result, true);
            if (Mathf.Abs(pointerDelta.y) > Mathf.Abs(pointerDelta.x) * DirectionPriority)
                return GuideLane(block, current, target, result, false);
            return result;
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

        Vector2 GuideLane(Block block, Vector2 current, Vector2 target, Vector2 normal, bool horizontal)
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
            return gain > PositionEpsilon ? guided : normal;
        }
    }
}
