using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed partial class BlockBoard
    {
        [SerializeField] Material placementPreviewMaterial;
        GameObject placementPreview;
        MeshFilter placementMesh;
        MeshRenderer placementRenderer;
        MaterialPropertyBlock placementProperties;
        Block previewBlock;
        FloorShadowTrail floorShadowTrail;
        readonly PlacementPreviewMeshCache placementMeshes = new PlacementPreviewMeshCache();
        static readonly int PreviewBaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int PreviewBaseColor = Shader.PropertyToID("_BaseColor");
        Texture previewTexture;
        Color previewColor;
        bool previewPropertiesApplied;
        bool previewVisible;
        Vector3 previewTarget, previewVelocity;
        float previewAlpha;

        public void SetPlacementPreviewMaterial(Material material)
        {
            placementPreviewMaterial = material;
        }

        public void BeginFloorShadowTrail(Block block)
        {
            if (block == null || layout == null || !Application.isPlaying) return;
            if (floorShadowTrail == null)
            {
                floorShadowTrail = gameObject.AddComponent<FloorShadowTrail>();
                floorShadowTrail.Initialize(layout.transform, layout.columns, layout.rows,
                    placementPreviewMaterial, placementMeshes);
            }
            floorShadowTrail.Begin(block, WorldToGrid(block.transform.position));
        }

        public void CaptureFloorShadowTrail(Block block)
        {
            if (floorShadowTrail != null && block != null)
                floorShadowTrail.Capture(block, WorldToGrid(block.transform.position));
        }

        public void ShowPlacementPreview(Block block, float tolerance)
        {
            if (!Application.isPlaying || placementPreviewMaterial == null || block.ColorData == null) return;
            Vector2 position = WorldToGrid(block.transform.position);
            if (!TrySnap(block, position, tolerance, out Vector2Int destination))
            {
                HidePlacementPreview(block);
                return;
            }
            if (placementPreview == null) CreatePlacementPreview();
            previewTarget = new Vector3(destination.x - (layout.columns - 1) * .5f,
                -.185f, (layout.rows - 1) * .5f - destination.y);
            if (previewBlock != block)
            {
                placementMesh.sharedMesh = placementMeshes.Get(block.Cells);
                previewBlock = block;
                previewAlpha = 0;
                previewVelocity = Vector3.zero;
                placementPreview.transform.localPosition = previewTarget;
            }
            Material source = block.ColorData.Material;
            Texture texture = source != null && source.HasProperty(PreviewBaseMap)
                ? source.GetTexture(PreviewBaseMap) : null;
            Color color = source != null && source.HasProperty(PreviewBaseColor)
                ? source.GetColor(PreviewBaseColor) : Color.white;
            texture = texture != null ? texture : Texture2D.whiteTexture;
            if (!previewPropertiesApplied || previewTexture != texture || previewColor != color)
            {
                placementProperties.Clear();
                placementProperties.SetTexture(PreviewBaseMap, texture);
                placementProperties.SetColor(PreviewBaseColor, color);
                placementRenderer.SetPropertyBlock(placementProperties);
                previewTexture = texture;
                previewColor = color;
                previewPropertiesApplied = true;
            }
            if (!placementPreview.activeSelf) placementPreview.transform.localPosition = previewTarget;
            previewVisible = true;
        }

        void CreatePlacementPreview()
        {
            placementPreview = new GameObject("PlacementPreview");
            placementPreview.transform.SetParent(layout.transform, false);
            placementMesh = placementPreview.AddComponent<MeshFilter>();
            placementRenderer = placementPreview.AddComponent<MeshRenderer>();
            placementRenderer.sharedMaterial = placementPreviewMaterial;
            placementRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            placementRenderer.receiveShadows = false;
            placementProperties = new MaterialPropertyBlock();
            placementPreview.SetActive(false);
        }

        public void HidePlacementPreview(Block block)
        {
            if (previewBlock == block) previewVisible = false;
        }

        void TickPlacementPreview(float deltaTime)
        {
            if (placementPreview == null) return;
            if (previewBlock == null || !previewBlock.IsDragging) previewVisible = false;
            float targetAlpha = previewVisible ? 1 : 0;
            previewAlpha = Mathf.MoveTowards(previewAlpha, targetAlpha, deltaTime / .075f);
            if (previewAlpha <= 0 && !previewVisible)
            {
                if (placementPreview.activeSelf) placementPreview.SetActive(false);
                return;
            }
            if (!placementPreview.activeSelf) placementPreview.SetActive(true);
            placementPreview.transform.localPosition = Vector3.SmoothDamp(placementPreview.transform.localPosition,
                previewTarget, ref previewVelocity, .06f, Mathf.Infinity, deltaTime);
            Color tint = previewColor;
            tint.a *= Mathf.SmoothStep(0, 1, previewAlpha);
            placementProperties.SetColor(PreviewBaseColor, tint);
            placementRenderer.SetPropertyBlock(placementProperties);
        }

        void OnDestroy()
        {
            matchSweeps?.Dispose();
            placementMeshes.Dispose();
            if (placementPreview != null) Destroy(placementPreview);
        }
    }
}
