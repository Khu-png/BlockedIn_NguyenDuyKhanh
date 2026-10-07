using System;
using BlockedIn.CameraTools;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardPreview
    {
        public static void FrameScene(GeneratedBoard board)
        {
            if (board == null) return;
            Selection.activeGameObject = board.gameObject;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        }

        public static void FitCamera(GeneratedBoard board, Camera camera)
        {
            if (board == null || camera == null) throw new InvalidOperationException("Assign a board and camera first.");
            if (EditorUtility.IsPersistent(camera) || camera.gameObject.scene != board.gameObject.scene)
                throw new InvalidOperationException("Assign a camera in the same scene as the board.");
            Undo.RecordObject(camera, "Fit board camera");
            Undo.RecordObject(camera.transform, "Fit board camera");
            CameraManager.Fit(camera, board, new Rect(0, 0, 1, 1));
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            if (!Application.isBatchMode) EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }
}
