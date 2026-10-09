using UnityEngine;

namespace BlockedIn.MapTools
{
    public enum BoardCell { Empty, Floor, Wall }

    // Keeps existing level assets compatible with LevelData.
    [CreateAssetMenu(menuName = "Blocked In/Level Data", fileName = "Level")]
    public sealed class BoardMap : LevelData { }
}
