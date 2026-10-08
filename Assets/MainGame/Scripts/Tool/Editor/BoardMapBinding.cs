using System;
using BlockedIn.MapTools;
using UnityEditor;
using UnityEngine;

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
            if (GeneratedBoard.TryResolveEditorId(targetEditorId, out GeneratedBoard board)) target = board;
            Repaint();
        }

        void RequireBoardBinding()
        {
            if (target == null && !string.IsNullOrEmpty(targetEditorId))
                throw new InvalidOperationException("The previous board is not available. Assign a Generated Board or clear the field to create a new board.");
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
            if (target == null && !string.IsNullOrEmpty(targetEditorId))
            {
                EditorGUILayout.HelpBox("The previous board is unavailable. Assign its scene component or reset the board reference.", MessageType.Warning);
                if (GUILayout.Button("Reset Board Reference")) SetBoardBinding(null);
            }
        }
    }
}
