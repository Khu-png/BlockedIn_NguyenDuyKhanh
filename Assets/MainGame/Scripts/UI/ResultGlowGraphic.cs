using UnityEngine;
using UnityEngine.UI;

public sealed class ResultGlowGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI / 12;
            float b = a + Mathf.PI / 19;
            Color inner = color; inner.a *= .3f;
            Color outer = color; outer.a = 0;
            int start = mesh.currentVertCount;
            Add(center + Direction(a) * radius * .12f, inner);
            Add(center + Direction(b) * radius * .12f, inner);
            Add(center + Direction(b) * radius, outer);
            Add(center + Direction(a) * radius, outer);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
        void Add(Vector2 point, Color tint) => mesh.AddVert(point, tint, Vector2.zero);
    }

    static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
}
