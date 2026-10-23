namespace BlockedIn.Blocks
{
    public static class BlockConnectionRules
    {
        public static bool CanMatch(Block first, Block second)
        {
            if (first == null || second == null || first == second || !first.HasSameColor(second)) return false;
            if (!first.HasGoalPosition || !second.HasGoalPosition) return false;
            // Both pieces may move anywhere, provided their solution-relative alignment is preserved.
            return first.GridPosition - first.GoalPosition == second.GridPosition - second.GoalPosition;
        }
    }
}
