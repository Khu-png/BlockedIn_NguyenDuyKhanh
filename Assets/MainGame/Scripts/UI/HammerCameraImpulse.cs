using UnityEngine;

public sealed class HammerCameraImpulse
{
    const float Duration = .24f;
    Camera camera;
    Vector3 offset, appliedPosition;
    float elapsed, strength;

    public void Play(Camera target)
    {
        Restore();
        camera = target;
        elapsed = 0;
        strength = target != null ? Mathf.Clamp(target.orthographicSize * .014f, .035f, .09f) : 0;
    }

    public void ClearOffset()
    {
        if (camera != null && (camera.transform.position - appliedPosition).sqrMagnitude < .000001f)
            camera.transform.position -= offset;
        offset = Vector3.zero;
    }

    public void Tick(float deltaTime)
    {
        ClearOffset();
        if (camera == null || elapsed >= Duration) return;
        elapsed += Mathf.Max(0, deltaTime);
        float fade = Mathf.Pow(1 - Mathf.Clamp01(elapsed / Duration), 2);
        offset = (camera.transform.right * Mathf.Cos(elapsed * 96)
            + camera.transform.up * Mathf.Sin(elapsed * 77 + .7f)) * (strength * fade);
        camera.transform.position += offset;
        appliedPosition = camera.transform.position;
    }

    public void Restore()
    {
        ClearOffset();
        camera = null;
        elapsed = Duration;
    }
}
