using UnityEngine;
namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        static readonly int HammerSelected = Shader.PropertyToID("_HammerSelected");
        MaterialPropertyBlock hammerProperties;
        public void ShowHammerSelection(bool active, bool eligible)
        {
            if (hammerProperties == null) hammerProperties = new MaterialPropertyBlock();
            foreach (Renderer item in renderers)
            {
                if (item == null) continue;
                item.GetPropertyBlock(hammerProperties);
                hammerProperties.SetFloat(HammerSelected, active && eligible ? 1 : 0);
                item.SetPropertyBlock(hammerProperties);
            }
            Color tint = colorData != null ? colorData.IconTint : Color.white;
            if (active && !eligible) tint *= .28f;
            tint.a = 1;
            foreach (SpriteRenderer icon in iconRenderers)
                if (icon != null) icon.color = tint;
        }
    }
}
