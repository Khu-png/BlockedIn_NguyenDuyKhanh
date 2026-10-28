using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class HammerImpactBurst : IDisposable
{
    const float Duration = .42f;
    static readonly int Progress = Shader.PropertyToID("_ImpactProgress");
    readonly GameObject root;
    readonly Mesh mesh;
    readonly MeshRenderer renderer;
    readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    float elapsed = Duration;

    public HammerImpactBurst(Material material)
    {
        root = new GameObject("Hammer Impact Burst");
        mesh = new Mesh { name = "Hammer Impact Surface" };
        mesh.vertices = new[] { new Vector3(-1,-1,0), new Vector3(1,-1,0), new Vector3(1,1,0), new Vector3(-1,1,0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        root.SetActive(false);
    }

    public void Play(Vector3 point, Quaternion facing, float radius)
    {
        root.transform.SetPositionAndRotation(point, facing);
        root.transform.localScale = Vector3.one * radius;
        elapsed = 0;
        Apply();
        root.SetActive(true);
    }

    public void Tick(float deltaTime)
    {
        if (elapsed >= Duration) return;
        elapsed = Mathf.Min(Duration, elapsed + Mathf.Max(0, deltaTime));
        if (elapsed >= Duration) root.SetActive(false);
        else Apply();
    }

    void Apply()
    {
        properties.SetFloat(Progress, elapsed / Duration);
        renderer.SetPropertyBlock(properties);
    }

    public void Clear()
    {
        elapsed = Duration;
        if (root != null) root.SetActive(false);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(mesh);
        if (root != null) UnityEngine.Object.Destroy(root);
    }
}
