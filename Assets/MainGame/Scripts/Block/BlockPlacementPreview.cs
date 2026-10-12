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
                if (placementMesh.sharedMesh != null) Destroy(placementMesh.sharedMesh);
                placementMesh.sharedMesh = BlockPlacementMesh.Build(block.Cells);
                previewBlock = block;
            }
            Material source = block.ColorData.Material;
            placementProperties.Clear();
            Texture texture = source != null && source.HasProperty("_BaseMap")
                ? source.GetTexture("_BaseMap") : null;
            Color color = source != null && source.HasProperty("_BaseColor")
                ? source.GetColor("_BaseColor") : Color.white;
            placementProperties.SetTexture("_BaseMap", texture != null ? texture : Texture2D.whiteTexture);
            placementProperties.SetColor("_BaseColor", color);
            placementRenderer.SetPropertyBlock(placementProperties);
            Vector3 local = layout.transform.InverseTransformPoint(GridToWorld(destination));
            local.y = -.19f;
            placementPreview.transform.localPosition = local;
            placementPreview.SetActive(true);
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
            if (placementMesh != null && placementMesh.sharedMesh != null) Destroy(placementMesh.sharedMesh);
            if (placementPreview != null) Destroy(placementPreview);
        }
    }
}
