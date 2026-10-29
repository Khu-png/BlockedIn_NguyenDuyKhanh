using UnityEngine;

namespace BlockedIn.Blocks
{
    sealed class BlockTiltMotion
    {
        readonly Transform block;
        readonly Transform visual;
        readonly Transform layout;
        readonly Quaternion restRotation;
        Camera camera;
        Vector3 lastPosition, tilt, velocity;
        float horizontalAngle, verticalAngle, rollAngle;

        public BlockTiltMotion(Transform owner, Transform model, Transform boardLayout,
            float horizontal, float vertical, float roll)
        {
            block = owner;
            visual = model;
            layout = boardLayout;
            restRotation = model.localRotation;
            Configure(horizontal, vertical, roll);
            Reset();
        }

        public void Configure(float horizontal, float vertical, float roll)
        {
            horizontalAngle = horizontal;
            verticalAngle = vertical;
            rollAngle = roll;
        }

        public void Begin(Camera dragCamera)
        {
            camera = dragCamera;
            lastPosition = block.position;
        }

        public void Tick(bool dragging, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            Vector3 movement = block.position - lastPosition;
            lastPosition = block.position;
            Vector3 target = dragging ? Target(movement / deltaTime) : Vector3.zero;
            float remaining = Mathf.Min(deltaTime, .1f);
            float leanLimit = Mathf.Max(horizontalAngle, verticalAngle);
            float maxAngle = Mathf.Sqrt(leanLimit * leanLimit + rollAngle * rollAngle);
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, .008f);
                velocity += ((target - tilt) * 180f - velocity * 14f) * step;
                tilt += velocity * step;
                tilt = Vector3.ClampMagnitude(tilt, maxAngle);
                remaining -= step;
            }
            Quaternion rotation = tilt.sqrMagnitude > .000001f
                ? Quaternion.AngleAxis(tilt.magnitude, tilt.normalized) : Quaternion.identity;
            Quaternion parentRotation = visual.parent != null ? visual.parent.rotation : Quaternion.identity;
            visual.localRotation = Quaternion.Inverse(parentRotation) * rotation * parentRotation * restRotation;
        }

        Vector3 Target(Vector3 speed)
        {
            Vector3 direction = Vector3.ClampMagnitude(speed / 4f, 1f);
            Vector3 forward = camera != null ? camera.transform.forward : -layout.up;
            Vector3 cameraRight = camera != null ? camera.transform.right : layout.right;
            Vector3 right = Vector3.ProjectOnPlane(cameraRight, layout.up).normalized;
            Vector3 up = Vector3.Cross(right, layout.up).normalized;
            float horizontal = Vector3.Dot(direction, right);
            float vertical = Mathf.Clamp(Vector3.Dot(speed, up) / .8f, -1f, 1f);
            Vector3 lean = Vector3.Cross(layout.up, right) * horizontal * horizontalAngle
                + Vector3.Cross(layout.up, up) * vertical * verticalAngle;
            return lean - forward * horizontal * rollAngle;
        }

        public void Reset()
        {
            camera = null;
            tilt = velocity = Vector3.zero;
            lastPosition = block.position;
            if (visual != null) visual.localRotation = restRotation;
        }
    }
}
