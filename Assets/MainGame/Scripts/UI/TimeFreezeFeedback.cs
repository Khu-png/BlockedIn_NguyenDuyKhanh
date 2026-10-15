using UnityEngine;
using UnityEngine.UI;

public sealed class TimeFreezeFeedback : MonoBehaviour
{
    [SerializeField] Image[] sparkles = System.Array.Empty<Image>();
    [SerializeField] Image iconGlow;
    [SerializeField] Image barGlow;
    [SerializeField] Image barCore;
    float[] sparkleLife;
    Vector3[] sparkleVelocity;
    int nextSparkle;
    Vector3 lastPosition;
    float emissionTime;
    bool hasPosition;
    bool particlesActive;

    void Awake() => Clear();
    void EnsureState()
    {
        if (sparkleLife != null) return;
        sparkleLife = new float[sparkles.Length];
        sparkleVelocity = new Vector3[sparkles.Length];
    }
    public void BeginFlight()
    {
        Clear();
        hasPosition = false;
        emissionTime = 0f;
    }
    public void SampleFlight(Vector3 position, float progress, float delta)
    {
        EnsureState();
        if (iconGlow != null)
        {
            iconGlow.enabled = true;
            iconGlow.color = new Color(1f, .7f, .2f, .22f * Mathf.Sin(progress * Mathf.PI));
        }
        if (!hasPosition) { lastPosition = position; hasPosition = true; return; }
        emissionTime += delta;
        Vector3 direction = position - lastPosition;
        if (direction.sqrMagnitude < 16f * 16f && emissionTime < .035f) return;
        if (progress < .85f)
        {
            EmitSparkle(position);
        }
        lastPosition = position;
        emissionTime = 0f;
    }
    void EmitSparkle(Vector3 position)
    {
        if (sparkles.Length == 0) return;
        int index = nextSparkle++ % sparkles.Length;
        Image image = sparkles[index];
        image.enabled = true;
        image.rectTransform.localPosition = position + new Vector3(Mathf.Sin(nextSparkle * 2.4f) * 36f, 0f, 0f);
        image.color = Color.Lerp(new Color(1f, .45f, .08f, 1f), new Color(1f, .85f, .35f, 1f), index % 4 / 3f);
        sparkleLife[index] = .45f;
        particlesActive = true;
        sparkleVelocity[index] = new Vector3(Mathf.Cos(nextSparkle * 2.4f) * 60f, 25f, 0f);
    }
    void Update()
    {
        if (particlesActive) Tick(Time.deltaTime);
    }
    void Tick(float delta)
    {
        EnsureState();
        particlesActive = false;
        for (int i = 0; i < sparkles.Length; i++)
        {
            if (sparkleLife[i] <= 0f) continue;
            sparkleLife[i] = Mathf.Max(0f, sparkleLife[i] - delta);
            if (sparkleLife[i] > 0f) particlesActive = true;
            float life = sparkleLife[i] / .45f;
            Image image = sparkles[i];
            image.rectTransform.localPosition += sparkleVelocity[i] * delta;
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1f - life) * 120f);
            image.rectTransform.sizeDelta = Vector2.one * ((18f + i % 4 * 7f) * Mathf.Sin(life * Mathf.PI));
            Color color = image.color; color.a = Mathf.Sin(life * Mathf.PI);
            image.color = color; image.enabled = life > 0f;
        }
    }
    public void EndFlight()
    {
        if (iconGlow != null) { iconGlow.enabled = false; iconGlow.color = Color.clear; }
    }
    public void SetBarProgress(float progress)
    {
        SetLight(barGlow, progress > 0f, .6f + Mathf.Sin(Time.time * 7f) * .1f);
        SetLight(barCore, progress > 0f, .9f);
    }
    static void SetLight(Image image, bool visible, float alpha)
    {
        if (image == null) return;
        image.enabled = visible;
        image.color = new Color(.4f, 1f, 1f, alpha);
    }
    public void Clear()
    {
        EnsureState();
        particlesActive = false;
        for (int i = 0; i < sparkles.Length; i++) { sparkleLife[i] = 0f; sparkles[i].enabled = false; }
        EndFlight();
        SetBarProgress(0f);
    }
    void OnDisable() => Clear();
}
