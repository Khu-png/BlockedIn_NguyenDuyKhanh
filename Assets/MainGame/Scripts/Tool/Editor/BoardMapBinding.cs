using System;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockedIn.EditorTools
{
    public sealed partial class BoardMapWindow
    {
        [SerializeField] string targetEditorId;

        void EnableBoardBinding()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += RestoreBoardBinding;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            RememberBoardBinding();
            EditorApplication.delayCall += RestoreBoardBinding;
        }

        void DisableBoardBinding()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= RestoreBoardBinding;
            EditorApplication.delayCall -= RestoreBoardBinding;
        }

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) RememberBoardBinding();
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += RestoreBoardBinding;
        }

        void RememberBoardBinding()
        {
            if (target != null && !Application.IsPlaying(target.gameObject)) targetEditorId = target.EditorId;
        }

        void RestoreBoardBinding()
        {
            if (this == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (target != null && target.gameObject.scene == scene)
                RememberBoardBinding();
            else if (GeneratedBoard.TryResolveEditorId(targetEditorId, out GeneratedBoard board)
                && board.gameObject.scene == scene)
                SetBoardBinding(board);
            else
            {
                int count = GeneratedBoard.RegisteredSceneBoards(scene, out GeneratedBoard onlyBoard);
                if (count == 1) SetBoardBinding(onlyBoard);
                else if (count == 0) SetBoardBinding(null);
                else target = null;
            }
            Repaint();
        }

        void RequireBoardBinding()
        {
            if (target == null && GeneratedBoard.RegisteredSceneBoards(SceneManager.GetActiveScene(), out _) > 1)
                throw new InvalidOperationException("More than one generated board exists in the active scene. Assign the board you want to update.");
        }

        void SetBoardBinding(GeneratedBoard board)
        {
            target = board;
            targetEditorId = board != null ? board.EditorId : null;
        }

        void DrawBoardBinding()
        {
            RestoreBoardBinding();
            EditorGUI.BeginChangeCheck();
            var board = (GeneratedBoard)EditorGUILayout.ObjectField("Generated board", target, typeof(GeneratedBoard), true);
            if (EditorGUI.EndChangeCheck()) SetBoardBinding(board);
            if (target == null && GeneratedBoard.RegisteredSceneBoards(SceneManager.GetActiveScene(), out _) > 1)
                EditorGUILayout.HelpBox("Multiple generated boards are available. Assign the board you want to update.", MessageType.Warning);
        }
    }
}
