using UnityEngine;

namespace BlockedIn.MapTools
{
    public enum BoardCell { Empty, Floor, Wall }

    [CreateAssetMenu(menuName = "Blocked In/Board Map", fileName = "BoardMap")]
    public sealed class BoardMap : ScriptableObject
    {
        [Min(1)] public int levelNumber = 1;
        [Min(1)] public int columns = 4;
        [Min(1)] public int rows = 4;
        public bool outerWalls = true;
        public BoardCell[] cells =
        {
            BoardCell.Floor, BoardCell.Floor, BoardCell.Floor, BoardCell.Floor,
            BoardCell.Floor, BoardCell.Floor, BoardCell.Floor, BoardCell.Floor,
            BoardCell.Floor, BoardCell.Floor, BoardCell.Floor, BoardCell.Floor,
            BoardCell.Floor, BoardCell.Floor, BoardCell.Floor, BoardCell.Floor
        };

        public bool IsValid => columns >= 1 && columns <= 32 && rows >= 1 && rows <= 32
            && cells != null && cells.Length == columns * rows;
    }
}
