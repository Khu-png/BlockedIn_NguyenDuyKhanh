using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class FreezeCornerSnow : MaskableGraphic
{
    [SerializeField] RectTransform[] corners = System.Array.Empty<RectTransform>();
    [SerializeField, Min(0)] float emissionRate = 6f;
    const int Capacity = 40;
    struct Flake
    {
        public Vector2 position, velocity;
        public float age, lifetime, size, angle, spin;
    }
    readonly Flake[] flakes = new Flake[Capacity];
    bool frozen;
    int next, nextCorner;
    float emission;

    public void SetFrozen(bool value)
    {
        if (value == frozen) return;
        frozen = value;
        emission = 0;
        if (!value) return;
        for (int i = 0; i < corners.Length * 3; i++) Emit();
        SetVerticesDirty();
    }
    void Emit()
    {
        if (corners.Length == 0) return;
        RectTransform corner = corners[nextCorner++ % corners.Length];
        if (corner == null) return;
        Rect area = corner.rect;
        Vector2 origin = new Vector2(
            Mathf.Lerp(area.xMin, area.xMax, Random.Range(.2f, .85f)),
            Mathf.Lerp(area.yMin, area.yMax, Random.Range(.2f, .85f)));
        Vector2 position = rectTransform.InverseTransformPoint(corner.TransformPoint(origin));
        Vector2 inward = (rectTransform.rect.center - position).normalized;
        float scale = Mathf.Clamp(rectTransform.rect.width / 1080f, .5f, 2f);
        flakes[next++ % Capacity] = new Flake
        {
            position = position,
            velocity = (inward * Random.Range(35f, 75f)
                + new Vector2(Random.Range(-15f, 15f), Random.Range(-25f, 5f))) * scale,
            lifetime = Random.Range(1.8f, 3.1f),
            size = Random.Range(16f, 30f) * scale,
            angle = Random.Range(0f, Mathf.PI * 2f),
            spin = Random.Range(-.65f, .65f)
        };
    }
    void Update()
    {
        float delta = Time.deltaTime;
        if (delta <= 0f) return;
        bool changed = false;
        for (int i = 0; i < flakes.Length; i++)
        {
            Flake flake = flakes[i];
            if (flake.lifetime <= 0f) continue;
            changed = true;
            flake.age += delta;
            if (flake.age >= flake.lifetime) flake.lifetime = 0;
            else
            {
                flake.position += (flake.velocity
                    + Vector2.right * Mathf.Sin(flake.age * 2.5f + i) * 9f) * delta;
                flake.angle += flake.spin * delta;
            }
            flakes[i] = flake;
        }
        if (frozen)
        {
            emission += delta * emissionRate;
            int count = Mathf.Min(Capacity, Mathf.FloorToInt(emission));
            emission -= count;
            for (int i = 0; i < count; i++) Emit();
            changed |= count > 0;
        }
        if (changed) SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        foreach (Flake flake in flakes)
        {
            if (flake.lifetime <= 0f) continue;
            float fade = Mathf.Clamp01(flake.age / .18f)
                * Mathf.Clamp01((flake.lifetime - flake.age) / .75f);
            Color tint = Color.Lerp(new Color(.45f, .85f, 1f), Color.white, .65f);
            tint *= color;
            tint.a *= fade * .8f;
            DrawFlake(mesh, flake, tint);
        }
    }
    static void DrawFlake(VertexHelper mesh, Flake flake, Color tint)
    {
        float radius = flake.size * .5f;
        for (int arm = 0; arm < 6; arm++)
        {
            float angle = flake.angle + arm * Mathf.PI / 3f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 tip = flake.position + direction * radius;
            Line(mesh, flake.position, tip, radius * .11f, tint);
            Vector2 joint = flake.position + direction * radius * .6f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 branch = joint + direction * radius * .25f
                    + normal * (radius * .27f * side);
                Line(mesh, joint, branch, radius * .085f, tint);
            }
        }
    }
    static void Line(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color tint)
    {
        Vector2 tangent = (end - start).normalized;
        Vector2 offset = new Vector2(-tangent.y, tangent.x) * width;
        int index = mesh.currentVertCount;
        mesh.AddVert(start - offset, tint, Vector2.zero);
        mesh.AddVert(start + offset, tint, Vector2.zero);
        mesh.AddVert(end + offset, tint, Vector2.zero);
        mesh.AddVert(end - offset, tint, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
    public void Clear()
    {
        frozen = false;
        emission = 0;
        next = nextCorner = 0;
        System.Array.Clear(flakes, 0, flakes.Length);
        SetVerticesDirty();
    }
    protected override void OnDisable()
    {
        Clear();
        base.OnDisable();
    }
}
