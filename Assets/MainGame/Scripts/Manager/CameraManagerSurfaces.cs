using UnityEngine;
using BlockedIn.MapTools;
namespace BlockedIn.CameraTools
{
    public sealed partial class CameraManager
    {
        static readonly int PanelSize = Shader.PropertyToID("_PanelSize");
        static readonly int BoardBounds = Shader.PropertyToID("_GameplayBoardBounds");
        static readonly int BoardWorldToLocal = Shader.PropertyToID("_GameplayBoardWorldToLocal");
        static readonly int BoardGrid = Shader.PropertyToID("_GameplayBoardGrid");
        static readonly int BoardCells = Shader.PropertyToID("_GameplayBoardCells");
        static readonly int BoardShape = Shader.PropertyToID("_BoardShape");
        static readonly int ResultFade = Shader.PropertyToID("_ResultFade");
        MaterialPropertyBlock frameProperties;
        GeneratedBoard surfaceBoard;
        BoardCell[] surfaceCells;
        Texture2D floorMask, outlineMask;
        public void SetResultFrameVisible(bool visible)
            => SetResultFrameProgress(visible ? 1 : 0);
        public void SetResultFrameProgress(float visible)
        {
            if (boardFrame == null) return;
            visible = Mathf.Clamp01(visible);
            boardFrame.enabled = visible > 0;
            boardFrame.transform.localScale = (boardFramePrefab != null
                ? boardFramePrefab.transform.localScale : Vector3.one) * Mathf.Lerp(.94f, 1, visible);
            if (frameProperties == null) frameProperties = new MaterialPropertyBlock();
            boardFrame.GetPropertyBlock(frameProperties);
            frameProperties.SetFloat(ResultFade, visible);
            boardFrame.SetPropertyBlock(frameProperties);
        }
        Sprite fittedBackgroundSprite;
        Vector3 fittedBackgroundSize;
        void FitBackground()
        {
            if (mainCamera == null || backgroundPrefab == null) return;
            if (background == null)
            {
                background = Instantiate(backgroundPrefab, mainCamera.transform);
                fittedBackgroundSprite = null;
            }
            if (background.sprite == null) return;
            Vector3 cameraSize = new Vector3(mainCamera.orthographicSize, mainCamera.aspect, mainCamera.farClipPlane);
            if (fittedBackgroundSprite == background.sprite && fittedBackgroundSize == cameraSize) return;
            fittedBackgroundSprite = background.sprite;
            fittedBackgroundSize = cameraSize;
            background.transform.localPosition = new Vector3(0, 0, mainCamera.farClipPlane - 1);
            background.transform.localRotation = Quaternion.identity;
            Vector2 size = background.sprite.bounds.size;
            float height = mainCamera.orthographicSize * 2;
            background.transform.localScale = new Vector3(height * mainCamera.aspect / size.x, height / size.y, 1);
        }
        void FitBoardFrame()
        {
            if (board == null) return;
            Shader.SetGlobalMatrix(BoardWorldToLocal, board.transform.worldToLocalMatrix);
            Shader.SetGlobalVector(BoardBounds, new Vector4(board.columns * .5f + .12f, board.rows * .5f + .12f, 1, 0));
            UpdateBoardShape();
            Shader.SetGlobalVector(BoardGrid, new Vector4(board.columns, board.rows, 0, 0));
            Shader.SetGlobalTexture(BoardCells, floorMask);
            if (boardFramePrefab == null) return;
            if (boardFrame == null) boardFrame = Instantiate(boardFramePrefab);
            boardFrame.transform.SetParent(board.transform, false);
            boardFrame.transform.localPosition = new Vector3(0, -.3f, 0);
            boardFrame.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Vector2 panel = new Vector2(board.columns + 2f, board.rows + 2f);
            boardFrame.drawMode = SpriteDrawMode.Sliced;
            boardFrame.size = panel;
            if (frameProperties == null) frameProperties = new MaterialPropertyBlock();
            boardFrame.GetPropertyBlock(frameProperties);
            frameProperties.SetVector(PanelSize, new Vector4(panel.x, panel.y, 0, 0));
            frameProperties.SetTexture(BoardShape, outlineMask);
            boardFrame.SetPropertyBlock(frameProperties);
        }
        void UpdateBoardShape()
        {
            if (surfaceBoard == board && surfaceCells == board.cells && floorMask != null
                && floorMask.width == board.columns && floorMask.height == board.rows) return;
            ReleaseBoardShape();
            surfaceBoard = board;
            surfaceCells = board.cells;
            floorMask = BoardSurfaceShape.CreateCells(board);
            outlineMask = BoardSurfaceShape.CreateOutline(board);
        }
        void ReleaseBoardShape()
        {
            ReleaseTexture(floorMask);
            ReleaseTexture(outlineMask);
            floorMask = null;
            outlineMask = null;
        }
        static void ReleaseTexture(Texture2D texture)
        {
            if (texture == null) return;
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
        void OnDestroy()
        {
            if (surfaceBoard != null && surfaceBoard == board)
                Shader.SetGlobalVector(BoardBounds, Vector4.zero);
            ReleaseBoardShape();
        }
    }
}
