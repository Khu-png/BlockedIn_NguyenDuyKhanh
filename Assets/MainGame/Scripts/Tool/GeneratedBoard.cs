using UnityEngine;
using BlockedIn.Blocks;

namespace BlockedIn.MapTools
{
    // Marks the exact scene object owned by the editor tool.
    [ExecuteAlways]
    public sealed class GeneratedBoard : MonoBehaviour
    {
        public int columns;
        public int rows;
        public bool outerWalls;
        public BoardCell[] cells;
        [SerializeField] Transform tiles;
        [SerializeField] Transform backdrop;
        [SerializeField] Transform walls;

        public BlockBoard blockGameplay;
        [SerializeField] BlockInput input;
        public BlockInput Input => input;
        public void SetInput(BlockInput controller) => input = controller;

        public Transform Tiles => tiles;
        public Transform Backdrop => backdrop;
        public Transform Walls => walls;

        public void SetGroups(Transform tileGroup, Transform backdropGroup, Transform wallGroup)
        {
            tiles = tileGroup;
            backdrop = backdropGroup;
            walls = wallGroup;
        }

#if UNITY_EDITOR
        [SerializeField, HideInInspector] string editorId;
        static readonly System.Collections.Generic.Dictionary<string, GeneratedBoard> editorBoards = new();
        public string EditorId
        {
            get { RegisterEditorBoard(); return editorId; }
        }

        void OnEnable() => RegisterEditorBoard();
        void OnValidate() => RegisterEditorBoard();

        void RegisterEditorBoard()
        {
            if (!gameObject.scene.IsValid() || Application.IsPlaying(gameObject)) return;
            bool duplicate = !string.IsNullOrEmpty(editorId) && editorBoards.TryGetValue(editorId, out GeneratedBoard previous)
                && previous != null && previous != this;
            if (string.IsNullOrEmpty(editorId) || duplicate) editorId = System.Guid.NewGuid().ToString("N");
            editorBoards[editorId] = this;
        }

        void OnDestroy()
        {
            if (string.IsNullOrEmpty(editorId)) return;
            if (editorBoards.TryGetValue(editorId, out GeneratedBoard current) && current == this)
                editorBoards.Remove(editorId);
        }

        public static int RegisteredSceneBoards(UnityEngine.SceneManagement.Scene scene, out GeneratedBoard onlyBoard)
        {
            onlyBoard = null;
            int count = 0;
            foreach (GeneratedBoard board in editorBoards.Values)
            {
                if (board == null || board.gameObject.scene != scene || Application.IsPlaying(board.gameObject)) continue;
                count++;
                onlyBoard = board;
            }
            if (count != 1) onlyBoard = null;
            return count;
        }

        public static bool TryResolveEditorId(string id, out GeneratedBoard board)
        {
            board = null;
            return !string.IsNullOrEmpty(id) && editorBoards.TryGetValue(id, out board) && board != null;
        }
#endif
    }
}
