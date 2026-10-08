using UnityEngine;

namespace BlockedIn.Blocks
{
    [CreateAssetMenu(menuName = "Blocked In/Block Color", fileName = "BlockColor")]
    public sealed class BlockColorData : ScriptableObject
    {
        [SerializeField, Min(0)] int colorId;
        [SerializeField] Material material;
        public int ColorId => colorId;
        public Material Material => material;

        public void Configure(int id, Material value)
        {
            colorId = id;
            material = value;
        }
    }
}
