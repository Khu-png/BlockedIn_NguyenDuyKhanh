using UnityEngine;
using UnityEngine.UI;

public sealed class WinTransitionOverlay
{
    public const int BlockLayer = 31;
    readonly Camera sourceCamera;
    readonly Transform canvasParent;
    Camera renderCamera;
    RenderTexture renderTexture;
    RawImage overlayImage;

    public WinTransitionOverlay(Camera sourceCamera, Transform canvasParent)
    {
        this.sourceCamera = sourceCamera;
        this.canvasParent = canvasParent;
    }

    public void Begin()
    {
        EnsureResources();
        SyncCamera();
        overlayImage.gameObject.SetActive(true);
        overlayImage.transform.SetAsFirstSibling();
    }

    public void Render()
    {
        SyncCamera();
        renderCamera.Render();
    }

    public void End()
    {
        if (overlayImage != null) overlayImage.gameObject.SetActive(false);
    }

    void EnsureResources()
    {
        if (renderCamera == null)
        {
            GameObject cameraObject = new GameObject("Win Transition Block Camera");
            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.CopyFrom(sourceCamera);
            renderCamera.enabled = false;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << BlockLayer;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = false;
            renderCamera.useOcclusionCulling = false;
        }

        if (renderTexture == null || renderTexture.width != Screen.width
            || renderTexture.height != Screen.height)
        {
            if (renderTexture != null)
            {
                renderCamera.targetTexture = null;
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
            renderTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32)
            {
                name = "Win Transition Block Overlay",
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            renderCamera.targetTexture = renderTexture;
            if (overlayImage != null) overlayImage.texture = renderTexture;
        }

        if (overlayImage != null) return;
        GameObject imageObject = new GameObject("Win Transition Block Overlay",
            typeof(RectTransform), typeof(CanvasRenderer));
        imageObject.transform.SetParent(canvasParent, false);
        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        overlayImage = imageObject.AddComponent<RawImage>();
        overlayImage.texture = renderTexture;
        overlayImage.raycastTarget = false;
    }

    void SyncCamera()
    {
        Transform sourceTransform = sourceCamera.transform;
        Transform renderTransform = renderCamera.transform;
        renderTransform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        renderCamera.orthographic = sourceCamera.orthographic;
        renderCamera.orthographicSize = sourceCamera.orthographicSize;
        renderCamera.fieldOfView = sourceCamera.fieldOfView;
        renderCamera.nearClipPlane = sourceCamera.nearClipPlane;
        renderCamera.farClipPlane = sourceCamera.farClipPlane;
        renderCamera.rect = sourceCamera.rect;
    }
}
