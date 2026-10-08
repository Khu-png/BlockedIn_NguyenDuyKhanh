using UnityEngine;
using BlockedIn.Blocks;

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

        public BlockColorData[] blockColors;
        public BlockEdgeData[] blockEdges;
        public int[] blockGroups;

        public bool IsValid => columns >= 1 && columns <= 32 && rows >= 1 && rows <= 32
            && cells != null && cells.Length == columns * rows
            && (blockColors == null || blockColors.Length == 0 || blockColors.Length == cells.Length)
            && (blockEdges == null || blockEdges.Length == 0 || blockEdges.Length == cells.Length)
            && (blockGroups == null || blockGroups.Length == 0 || blockGroups.Length == cells.Length);
    }
}
