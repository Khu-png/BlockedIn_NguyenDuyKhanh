using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup panel;
    [SerializeField] RectTransform progressFill;
    [SerializeField] Image fillImage;
    [SerializeField] Text progressText;

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        panel.alpha = 1;
        panel.blocksRaycasts = true;
        SetProgress(0);
    }

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        progressFill.anchorMax = new Vector2(value, 1);
        fillImage.enabled = value > 0;
        progressText.text = "Loading " + Mathf.RoundToInt(value * 100) + "%";
    }

    public void SetAlpha(float value) => panel.alpha = Mathf.Clamp01(value);
    public void Hide() => gameObject.SetActive(false);
}
