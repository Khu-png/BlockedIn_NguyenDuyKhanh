using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class Block
    {
        static readonly int TabOverIce = Shader.PropertyToID("_TabOverIce");
        MaterialPropertyBlock tabLayerProperties;

        void RefreshTabLayer()
        {
            if (renderers == null) return;
            if (tabLayerProperties == null) tabLayerProperties = new MaterialPropertyBlock();
            foreach (Renderer item in renderers)
            {
                if (item == null) continue;
                item.GetPropertyBlock(tabLayerProperties);
                tabLayerProperties.SetFloat(TabOverIce, IsFrozen ? 0 : 1);
                item.SetPropertyBlock(tabLayerProperties);
            }
        }
    }
}
