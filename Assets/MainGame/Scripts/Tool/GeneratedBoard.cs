using UnityEngine;

namespace BlockedIn.MapTools
{
    // Marks the exact scene object owned by the editor tool.
    public sealed class GeneratedBoard : MonoBehaviour
    {
        public int columns;
        public int rows;
        public bool outerWalls;
        public BoardCell[] cells;
        [SerializeField] Transform tiles;
        [SerializeField] Transform backdrop;
        [SerializeField] Transform walls;

        public Transform Tiles => tiles;
        public Transform Backdrop => backdrop;
        public Transform Walls => walls;

        public void SetGroups(Transform tileGroup, Transform backdropGroup, Transform wallGroup)
        {
            tiles = tileGroup;
            backdrop = backdropGroup;
            walls = wallGroup;
        }
    }
}
