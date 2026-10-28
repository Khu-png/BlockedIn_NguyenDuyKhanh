using UnityEngine;
using UnityEngine.UI;

public sealed class ResultConfetti
{
    const int Count = 84;
    readonly RectTransform[] pieces = new RectTransform[Count];
    readonly Image[] images = new Image[Count];
    readonly Color[] colors = { new Color(1,.75f,.08f), new Color(.08f,.9f,1),
        new Color(1,.2f,.55f), new Color(.52f,1,.08f), new Color(.68f,.35f,1),
        new Color(1,.35f,.12f), new Color(.2f,.55f,1) };

    public ResultConfetti(RectTransform root)
    {
        for (int i = 0; i < Count; i++)
        {
            var item = new GameObject("Celebration Piece");
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.SetParent(root, false);
            item.AddComponent<CanvasRenderer>();
            Image image = item.AddComponent<Image>();
            image.raycastTarget = false;
            rect.sizeDelta = PieceSize(i);
            pieces[i] = rect; images[i] = image;
        }
        Sample(-1);
    }

    public void Sample(float elapsed)
    {
        for (int i = 0; i < Count; i++)
        {
            float t = elapsed - .06f - i % 12 * .018f - i / 42 * .2f;
            bool visible = t >= 0 && t < 2.35f;
            images[i].enabled = visible;
            if (!visible) continue;
            float spread = (i * 37 % 101) / 100f;
            float direction = (i * 53 % 101) / 100f - .5f;
            float startX = ((i * 71 % 101) / 100f - .5f) * 250;
            pieces[i].anchoredPosition = new Vector2(startX + direction * (720 + spread * 300) * t,
                -330 + (820 + spread * 390) * t - 510 * t * t);
            pieces[i].localRotation = Quaternion.Euler(0, 0, i * 31 + t * (190 + i % 9 * 17));
            float flip = Mathf.Max(.22f, Mathf.Abs(Mathf.Cos(t * 9 + i)));
            pieces[i].localScale = new Vector3(flip, 1, 1);
            Color color = colors[i % colors.Length];
            color.a = 1 - Mathf.SmoothStep(0, 1, (t - 1.55f) / .8f);
            images[i].color = color;
        }
    }

    static Vector2 PieceSize(int index)
    {
        switch (index % 4)
        {
            case 0: return new Vector2(16, 16);
            case 1: return new Vector2(11, 29);
            case 2: return new Vector2(21, 9);
            default: return new Vector2(10, 23);
        }
    }
}
