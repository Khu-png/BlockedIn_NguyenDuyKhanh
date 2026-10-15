using UnityEngine;

namespace BlockedIn.Blocks
{
    [CreateAssetMenu(menuName = "Blocked In/Block Color", fileName = "BlockColor")]
    public sealed class BlockColorData : ScriptableObject
    {
        [SerializeField, Min(0)] int colorId;
        [SerializeField] Material material;
        [SerializeField] Sprite icon;
        public int ColorId => colorId;
        public Material Material => material;
        public Sprite Icon => icon;

        public void Configure(int id, Material value, Sprite symbol = null)
        {
            colorId = id;
            material = value;
            if (symbol != null) icon = symbol;
        }
    }
}
