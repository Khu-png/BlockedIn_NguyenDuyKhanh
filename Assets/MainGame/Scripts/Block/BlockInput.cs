using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
        UnityEngine.InputSystem.Controls.ButtonControl button;
        UnityEngine.InputSystem.Controls.Vector2Control position;
        int pointerId = -1;
        bool previousPressed;

        public void Configure(Camera camera, BlockBoard owner, EventSystem events = null)
        {
            mainCamera = camera;
            board = owner;
            uiEvents = events;
        }

        void Update()
        {
            if (mainCamera == null || board == null) return;
            if (selected == null && !ReadPointer()) return;
            if (button == null || position == null) { Release(); return; }
            Vector2 screen = position.ReadValue();
            bool pressed = button.isPressed;
            if (pressed && !previousPressed && selected == null) Press(screen);
            previousPressed = pressed;
            if (selected == null) return;
            if (!pressed) { Release(); return; }
            if (TryGetGrid(screen, out Vector2 grid)) selected.DragTo(grid + grabOffset);
        }

        bool ReadPointer()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var touch = Touchscreen.current.primaryTouch;
                button = touch.press;
                position = touch.position;
                pointerId = touch.touchId.ReadValue();
            }
            else if (Mouse.current != null)
            {
                button = Mouse.current.leftButton;
                position = Mouse.current.position;
                pointerId = -1;
            }
            else return false;
            return true;
        }

        void Press(Vector2 screen)
        {
            foreach (Block item in board.Blocks)
                if (item != null && item.IsSettling) return;
            if (uiEvents != null && uiEvents.IsPointerOverGameObject(pointerId)) return;
            Ray ray = mainCamera.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, blockMask, QueryTriggerInteraction.Ignore)) return;
            Block block = board.Resolve(hit.collider);
            if (block == null || !TryGetGrid(screen, out Vector2 grid) || !block.BeginDrag(mainCamera)) return;
            selected = block;
            grabOffset = board.WorldToGrid(block.transform.position) - grid;
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
            if (selected != null) selected.EndDrag();
            selected = null;
        }

        void OnDisable() => Release();
        void OnApplicationFocus(bool focused) { if (!focused) Release(); }
    }
}
