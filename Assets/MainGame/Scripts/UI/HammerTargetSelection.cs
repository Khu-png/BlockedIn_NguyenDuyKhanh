using System.Collections.Generic;
using BlockedIn.Blocks;
using UnityEngine;
using UnityEngine.UI;

public sealed class HammerTargetSelection
{
    static readonly int SelectionMode = Shader.PropertyToID("_HammerMode");
    readonly HashSet<Block> eligible = new HashSet<Block>();
    readonly Graphic[] hud;
    readonly Color[] colors;
    BlockBoard board;
    public bool IsActive { get; private set; }

    public HammerTargetSelection(Graphic[] graphics)
    {
        hud = graphics ?? System.Array.Empty<Graphic>();
        colors = new Color[hud.Length];
        for (int i = 0; i < hud.Length; i++)
            if (hud[i] != null) colors[i] = hud[i].color;
    }
    public bool Begin(BlockBoard owner)
    {
        if (IsActive || owner == null || owner.PowerupBusy) return false;
        eligible.Clear();
        foreach (Block block in owner.Blocks)
        {
            if (block == null) continue;
            if (block.IsDragging || block.IsSettling) return false;
            if (block.CanSplit) eligible.Add(block);
        }
        if (eligible.Count == 0) return false;
        board = owner;
        IsActive = true;
        Shader.SetGlobalFloat(SelectionMode, 1);
        foreach (Block block in board.Blocks)
            if (block != null) block.ShowHammerSelection(true, eligible.Contains(block));
        SetHudDimmed(true);
        return true;
    }
    public bool CanSelect(Block block) => block != null && eligible.Contains(block) && block.CanSplit;

    public void Clear()
    {
        if (!IsActive) return;
        Shader.SetGlobalFloat(SelectionMode, 0);
        if (board != null)
            foreach (Block block in board.Blocks)
                if (block != null) block.ShowHammerSelection(false, false);
        SetHudDimmed(false);
        board = null;
        IsActive = false;
        eligible.Clear();
    }
    void SetHudDimmed(bool dimmed)
    {
        for (int i = 0; i < hud.Length; i++)
        {
            if (hud[i] == null) continue;
            Color color = colors[i];
            if (dimmed) { color.r *= .3f; color.g *= .3f; color.b *= .3f; }
            hud[i].color = color;
        }
    }
}
