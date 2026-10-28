using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BlockedIn.Blocks
{
    public sealed class BlockInput : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] BlockBoard board;
        [SerializeField] EventSystem uiEvents;
        [SerializeField] LayerMask blockMask = ~0;
        Block selected;
        Vector2 grabOffset;
        TouchControl activeTouch;
        Mouse activeMouse;
        int pointerId = -1;
        PointerEventData uiPointer;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        public void Configure(Camera camera, BlockBoard owner, EventSystem events = null)
        {
            Release();
            mainCamera = camera;
            board = owner;
            uiEvents = events;
            uiPointer = null;
            uiHits.Clear();
        }

        void Update()
        {
            if (mainCamera == null || board == null) { Release(); return; }
            if (activeTouch != null) { UpdateTouch(); return; }
            if (activeMouse != null) { UpdateMouse(); return; }
            if (BeginTouch()) return;
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || !mouse.leftButton.isPressed) return;
            activeMouse = mouse;
            pointerId = -1;
            Press(mouse.position.ReadValue());
            Drag(mouse.position.ReadValue());
        }

        bool BeginTouch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return false;
            foreach (TouchControl touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                if (touch.phase.ReadValue() != TouchPhase.Began && !touch.press.wasPressedThisFrame) continue;
                activeTouch = touch;
                pointerId = touch.touchId.ReadValue();
                Press(touch.position.ReadValue());
                Drag(touch.position.ReadValue());
                return true;
            }
            return false;
        }

        void UpdateTouch()
        {
            if (!activeTouch.device.added) { Release(); return; }
            TouchPhase phase = activeTouch.phase.ReadValue();
            if (!activeTouch.press.isPressed
                || phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                Release();
                BeginTouch();
                return;
            }
            if (activeTouch.touchId.ReadValue() != pointerId
                || (phase == TouchPhase.Began && activeTouch.press.wasReleasedThisFrame))
            {
                Release();
                BeginTouch();
                return;
            }
            Drag(activeTouch.position.ReadValue());
        }

        void UpdateMouse()
        {
            if (!activeMouse.added || !activeMouse.leftButton.isPressed) { Release(); return; }
            Drag(activeMouse.position.ReadValue());
        }

        void Drag(Vector2 screen)
        {
            if (selected != null && TryGetGrid(screen, out Vector2 grid)) selected.DragTo(grid + grabOffset);
        }

        void Press(Vector2 screen)
        {
            foreach (Block item in board.Blocks)
                if (item != null && item.IsSettling) return;
            if (IsOverUI(screen)) return;
            Ray ray = mainCamera.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, blockMask, QueryTriggerInteraction.Ignore)) return;
            Block block = board.Resolve(hit.collider);
            if (board.PowerupPress != null && board.PowerupPress(block)) return;
            if (block != null && !block.CanDrag)
            {
                SoundManager.Play(block.IsRoped ? SoundManager.RopeLockedSound
                    : block.IsFrozen ? SoundManager.FrozenSound : SoundManager.LockedSound);
                return;
            }
            if (block == null || !TryGetGrid(screen, out Vector2 grid) || !block.BeginDrag(mainCamera)) return;
            board.NotifyBlockPressed(block);
            selected = block;
            grabOffset = board.WorldToGrid(block.transform.position) - grid;
        }

        bool IsOverUI(Vector2 screen)
        {
            if (uiEvents == null) return false;
            if (uiPointer == null) uiPointer = new PointerEventData(uiEvents);
            uiPointer.position = screen;
            uiPointer.pointerId = pointerId;
            uiHits.Clear();
            uiEvents.RaycastAll(uiPointer, uiHits);
            foreach (RaycastResult hit in uiHits)
                if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        public bool TryGetGrid(Vector2 screen, out Vector2 grid)
        {
            grid = default;
            var plane = new Plane(board.Layout.transform.up, board.Layout.transform.position);
            Ray ray = mainCamera.ScreenPointToRay(screen);
            if (!plane.Raycast(ray, out float distance)) return false;
            grid = board.WorldToGrid(ray.GetPoint(distance));
            return true;
        }

        void Release()
        {
            Block block = selected;
            selected = null;
            activeTouch = null;
            activeMouse = null;
            pointerId = -1;
            if (block != null) block.EndDrag();
        }

        void OnDisable() => Release();
        void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        void OnApplicationPause(bool paused) { if (paused) Release(); }
    }
}
