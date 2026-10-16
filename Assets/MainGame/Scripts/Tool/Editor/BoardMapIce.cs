using System;
using System.Collections.Generic;
using BlockedIn.Blocks;
using UnityEditor;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        [UnityEngine.SerializeField] BlockIceData[] frozenColors = Array.Empty<BlockIceData>();

        int IceCount(int index)
        {
            if (blockColors[index] == null || frozenColors == null) return 0;
            foreach (BlockIceData ice in frozenColors)
                if (ice.colorId == blockColors[index].ColorId) return ice.count;
            return 0;
        }

        void SetIceCount(int index, int count)
        {
            if (blockColors[index] == null) return;
            Undo.RecordObject(this, "Change frozen color");
            int id = blockColors[index].ColorId;
            var entries = new List<BlockIceData>(frozenColors ?? Array.Empty<BlockIceData>());
            entries.RemoveAll(ice => ice.colorId == id);
            if (count > 0) entries.Add(new BlockIceData { colorId = id, count = count });
            frozenColors = entries.ToArray();
            Repaint();
        }
    }
}
