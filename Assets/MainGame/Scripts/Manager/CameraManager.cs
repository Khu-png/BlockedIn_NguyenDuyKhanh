using System;
using BlockedIn.MapTools;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BlockedIn.CameraTools
{
    public sealed class CameraManager : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [Tooltip("Optional screen-aligned UI rectangle reserved for the board.")]
        [SerializeField] RectTransform gamePlayRect;
        [Tooltip("Leave empty for an Overlay Canvas. Otherwise assign the Canvas camera.")]
        [SerializeField] Camera uiCamera;
        [SerializeField, Min(1f)] float padding = 1.04f;
        [SerializeField] SpriteRenderer backgroundPrefab;
        [SerializeField] SpriteRenderer boardFramePrefab;
        SpriteRenderer background, boardFrame;
        GeneratedBoard board;
        Rect lastPixelRect;
        Rect lastViewport;
        float lastAspect;
        Vector2Int lastGridSize;
        Matrix4x4 lastBoardMatrix;
        float fittedSize, introProgress = 1;
        Vector3 fittedPosition;
        bool introActive;

        public Camera MainCamera => mainCamera;
        public void Configure(Camera camera, RectTransform area = null)
        {
            mainCamera = camera;
            gamePlayRect = area;
            uiCamera = null;
        }

        void LateUpdate()
        {
            FitBackground();
            if (board == null || mainCamera == null) return;
            if (mainCamera.pixelRect != lastPixelRect || mainCamera.aspect != lastAspect
                || GetViewport(mainCamera, gamePlayRect, uiCamera) != lastViewport
                || GridSize() != lastGridSize || board.transform.localToWorldMatrix != lastBoardMatrix)
                Fit();
        }

        public void FocusLevel(GeneratedBoard level)
        {
            if (board != level) { introActive = false; introProgress = 1; }
            board = level;
            Fit();
        }

        public void PrepareLevelIntro()
        {
            introActive = true;
            introProgress = 0;
            Fit();
        }

        public void SetLevelIntroProgress(float value)
        {
            introProgress = Mathf.Clamp01(value);
            if (introActive) ApplyLevelIntro();
            if (introProgress >= 1) introActive = false;
            FitBackground();
        }

        void ApplyLevelIntro()
        {
            if (mainCamera == null || board == null) return;
            float extraSize = fittedSize * .12f * (1 - introProgress);
            Vector2 offset = lastViewport.center - new Vector2(.5f, .5f);
            mainCamera.orthographicSize = fittedSize + extraSize;
            mainCamera.transform.position = fittedPosition
                - mainCamera.transform.right * (offset.x * extraSize * 2 * mainCamera.aspect)
                - mainCamera.transform.up * (offset.y * extraSize * 2);
        }

        public void Fit()
        {
            if (board == null) return;
            if (gamePlayRect != null) Canvas.ForceUpdateCanvases();
            Rect viewport = GetViewport(mainCamera, gamePlayRect, uiCamera);
            Fit(mainCamera, board, viewport, padding);
            fittedSize = mainCamera.orthographicSize;
            fittedPosition = mainCamera.transform.position;
            lastPixelRect = mainCamera.pixelRect;
            lastViewport = viewport;
            lastAspect = mainCamera.aspect;
            lastGridSize = GridSize();
            lastBoardMatrix = board.transform.localToWorldMatrix;
            if (introActive) ApplyLevelIntro();
            FitBackground();
            FitBoardFrame();
        }

        void FitBackground()
        {
            if (mainCamera == null || backgroundPrefab == null) return;
            if (background == null) background = Instantiate(backgroundPrefab, mainCamera.transform);
            if (background.sprite == null) return;
            background.transform.localPosition = new Vector3(0, 0, mainCamera.farClipPlane - 1);
            background.transform.localRotation = Quaternion.identity;
            Vector2 size = background.sprite.bounds.size;
            float height = mainCamera.orthographicSize * 2;
            background.transform.localScale = new Vector3(height * mainCamera.aspect / size.x, height / size.y, 1);
        }

        void FitBoardFrame()
        {
            if (board == null || boardFramePrefab == null) return;
            if (boardFrame == null) boardFrame = Instantiate(boardFramePrefab);
            boardFrame.transform.SetParent(board.transform, false);
            boardFrame.transform.localPosition = new Vector3(0, -.3f, 0);
            boardFrame.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            float border = board.outerWalls ? 2 : 0;
            boardFrame.drawMode = SpriteDrawMode.Sliced;
            boardFrame.size = new Vector2(board.columns + border + .4f, board.rows + border + .4f);
        }

        Vector2Int GridSize()
        {
            int border = board.outerWalls ? 2 : 0;
            return new Vector2Int(board.columns + border, board.rows + border);
        }

        public static Rect GetViewport(Camera camera, RectTransform area, Camera uiCamera)
        {
            if (camera == null) throw new InvalidOperationException("Assign Main Camera first.");
            if (area == null) return new Rect(0, 0, 1, 1);
            var corners = new Vector3[4];
            area.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            Rect pixels = camera.pixelRect;
            if (pixels.width <= 0 || pixels.height <= 0)
                throw new InvalidOperationException("Camera has no valid display area.");
            float left = Mathf.Clamp01((min.x - pixels.x) / pixels.width);
            float bottom = Mathf.Clamp01((min.y - pixels.y) / pixels.height);
            float right = Mathf.Clamp01((max.x - pixels.x) / pixels.width);
            float top = Mathf.Clamp01((max.y - pixels.y) / pixels.height);
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        public static void Fit(Camera camera, GeneratedBoard board, Rect viewport, float padding = 1.04f)
        {
            Validate(camera, board, viewport);
            Transform root = board.transform;
            float border = board.outerWalls ? 2 : 0;
            Vector3 horizontal = root.TransformVector(Vector3.right * (board.columns + border + .4f));
            Vector3 vertical = root.TransformVector(Vector3.forward * (board.rows + border + .4f));
            Quaternion rotation = Quaternion.LookRotation(-root.up, root.forward);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            float width = Mathf.Abs(Vector3.Dot(horizontal, right)) + Mathf.Abs(Vector3.Dot(vertical, right));
            float height = Mathf.Abs(Vector3.Dot(horizontal, up)) + Mathf.Abs(Vector3.Dot(vertical, up));
            float aspect = Mathf.Max(camera.aspect, .01f);
            float size = Mathf.Max(height / (2 * viewport.height), width / (2 * aspect * viewport.width));
            size = Mathf.Max(.01f, size * Mathf.Max(1, padding));
            float distance = Mathf.Max(horizontal.magnitude, vertical.magnitude) + 10;
            Vector2 offset = viewport.center - new Vector2(.5f, .5f);
            Vector3 position = root.position + root.up * distance;
            position -= right * (offset.x * size * 2 * aspect) + up * (offset.y * size * 2);
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, distance * .5f);
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, distance + 10);
        }

        static void Validate(Camera camera, GeneratedBoard board, Rect viewport)
        {
            if (camera == null || board == null) throw new InvalidOperationException("Assign a board and Main Camera first.");
            if (camera.gameObject.scene != board.gameObject.scene)
                throw new InvalidOperationException("Camera and board must belong to the same scene.");
            if (board.columns < 1 || board.rows < 1)
                throw new InvalidOperationException("Board dimensions must be greater than zero.");
            if (viewport.width <= .001f || viewport.height <= .001f || viewport.xMin < 0 || viewport.yMin < 0
                || viewport.xMax > 1 || viewport.yMax > 1)
                throw new InvalidOperationException("The board UI area must be inside the viewport and have a positive size.");
        }
    }
}

#if UNITY_EDITOR
namespace BlockedIn.CameraTools.Editor
{
    [CustomEditor(typeof(CameraManager))]
    public sealed class CameraManagerEditor : UnityEditor.Editor
    {
        GeneratedBoard previewBoard;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            previewBoard = (GeneratedBoard)EditorGUILayout.ObjectField("Preview board (Editor)", previewBoard, typeof(GeneratedBoard), true);
            var controller = (CameraManager)target;
            using (new EditorGUI.DisabledScope(previewBoard == null || controller.MainCamera == null || EditorUtility.IsPersistent(controller)))
            {
                if (!GUILayout.Button("Fit Camera")) return;
                try
                {
                    Undo.RecordObject(controller.MainCamera, "Fit board camera");
                    Undo.RecordObject(controller.MainCamera.transform, "Fit board camera");
                    controller.FocusLevel(previewBoard);
                    EditorSceneManager.MarkSceneDirty(controller.MainCamera.gameObject.scene);
                }
                catch (Exception exception)
                {
                    EditorUtility.DisplayDialog("Board Camera", exception.Message, "OK");
                }
            }
        }
    }
}

#endif
