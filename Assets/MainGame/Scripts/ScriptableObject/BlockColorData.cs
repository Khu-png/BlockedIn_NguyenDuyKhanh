using UnityEngine;

namespace BlockedIn.Blocks
{
    [CreateAssetMenu(menuName = "Blocked In/Block Color", fileName = "BlockColor")]
    public sealed class BlockColorData : ScriptableObject
    {
        [SerializeField, Min(0)] int colorId;
        [SerializeField] Material material;
        [SerializeField] bool isObstacle;
        [SerializeField] Sprite icon;
        [SerializeField] Color iconTint = Color.white;
        public int ColorId => colorId;
        public Material Material => material;
        public bool IsObstacle => isObstacle;
        public void SetObstacle(bool value) => isObstacle = value;
        public Sprite Icon => icon;
        public Color IconTint => iconTint;

        public void Configure(int id, Material value, Sprite symbol = null)
        {
            colorId = id;
            material = value;
            if (symbol != null) icon = symbol;
        }
    }
}
