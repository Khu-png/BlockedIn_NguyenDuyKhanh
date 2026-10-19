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
        readonly PlacementPreviewMeshCache placementMeshes = new PlacementPreviewMeshCache();
        static readonly int PreviewBaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int PreviewBaseColor = Shader.PropertyToID("_BaseColor");
        Texture previewTexture;
        Color previewColor;
        bool previewPropertiesApplied;

        public void SetPlacementPreviewMaterial(Material material)
        {
            placementPreviewMaterial = material;
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
            if (previewBlock != block)
            {
                placementMesh.sharedMesh = placementMeshes.Get(block.Cells);
                previewBlock = block;
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
            Vector3 local = new Vector3(destination.x - (layout.columns - 1) * .5f,
                -.19f, (layout.rows - 1) * .5f - destination.y);
            if (placementPreview.transform.localPosition != local) placementPreview.transform.localPosition = local;
            if (!placementPreview.activeSelf) placementPreview.SetActive(true);
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
        }

        public void HidePlacementPreview(Block block)
        {
            if (previewBlock == block && placementPreview != null) placementPreview.SetActive(false);
        }

        void OnDestroy()
        {
            placementMeshes.Dispose();
            if (placementPreview != null) Destroy(placementPreview);
        }
    }
}
