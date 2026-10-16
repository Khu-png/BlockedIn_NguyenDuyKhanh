using System;
using BlockedIn.MapTools;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        int paintStrokeGroup;
        int nextPaintGroup;
        int lastAppliedGroup;
        int lastPaintCell;

        void BeginPaintStroke()
        {
            paintStrokeGroup = 0;
            if (!UsesAssemblyWorkflow || assemblyStage != 0 || brushIndex != 2) return;
            foreach (int group in blockGroups) nextPaintGroup = Math.Max(nextPaintGroup, group);
            paintStrokeGroup = ++nextPaintGroup;
        }

        void PaintStrokeCell(int index, int group)
        {
            if (group <= 0) { PaintCell(index); return; }
            if (lastAppliedGroup != group)
            {
                lastAppliedGroup = group;
                lastPaintCell = index;
            }
            int current = lastPaintCell;
            while (true)
            {
                if (blockColors[current] != null && blockGroups[current] != group) return;
                if (blockColors[current] == null)
                {
                    PaintCell(current);
                    blockGroups[current] = group;
                }
                lastPaintCell = current;
                if (current == index) break;
                int x = current % columns, targetX = index % columns;
                current += x != targetX ? Math.Sign(targetX - x) : Math.Sign(index - current) * columns;
            }
            ClearInternalPorts(BlockShapeLayout.Members(index, blockGroups, blockColors));
        }
    }
}
