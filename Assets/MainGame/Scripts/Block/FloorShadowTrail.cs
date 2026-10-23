using UnityEngine;

namespace BlockedIn.Blocks
{
    sealed class FloorShadowTrail : MonoBehaviour
    {
        const int ShadowCount = 3;
        const float CellCrossingThreshold = .58f;
        const float FadeInDuration = .14f;
        const float FadeOutStart = .65f;
        const float Lifetime = 1.35f;
        const float BaseAlpha = .52f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly float[] trailStrength = { 1f, .54f, .23f };

        sealed class Shadow
        {
            public Transform transform;
            public MeshFilter filter;
            public MeshRenderer renderer;
        }

        readonly Shadow[] shadows = { new Shadow(), new Shadow(), new Shadow() };
        readonly Vector2Int[] trailCells = new Vector2Int[ShadowCount];
        readonly float[] trailAges = new float[ShadowCount];
        readonly float[] trailAlphas = new float[ShadowCount];
        Transform board;
        int columns;
        int rows;
        PlacementPreviewMeshCache meshCache;
        Material material;
        MaterialPropertyBlock properties;
        Block activeBlock;
        Vector2Int lastPosition;
        int trailCount;

        public void Initialize(Transform boardTransform, int boardColumns, int boardRows, Material shadowMaterial,
            PlacementPreviewMeshCache sharedMeshCache)
        {
            board = boardTransform;
            columns = boardColumns;
            rows = boardRows;
            material = shadowMaterial;
            meshCache = sharedMeshCache;
            properties = new MaterialPropertyBlock();
            for (int i = 0; i < shadows.Length; i++) CreateShadow(shadows[i], i);
        }

        public void Begin(Block block, Vector2 gridPosition)
        {
            activeBlock = block;
            lastPosition = Vector2Int.RoundToInt(gridPosition);
            trailCount = 0;
            System.Array.Clear(trailAges, 0, trailAges.Length);
            System.Array.Clear(trailAlphas, 0, trailAlphas.Length);
            foreach (Shadow shadow in shadows) shadow.renderer.gameObject.SetActive(false);
        }

        public void Capture(Block block, Vector2 gridPosition)
        {
            if (block == null || block != activeBlock || board == null || material == null || meshCache == null) return;
            Vector2Int destination = Vector2Int.RoundToInt(gridPosition);
            Vector2Int delta = destination - lastPosition;
            if (delta.x != 0 && Mathf.Abs(gridPosition.x - lastPosition.x) < CellCrossingThreshold)
                delta.x = 0;
            if (delta.y != 0 && Mathf.Abs(gridPosition.y - lastPosition.y) < CellCrossingThreshold)
                delta.y = 0;
            int steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            if (steps == 0) return;

            Vector2Int start = lastPosition;
            for (int step = 1; step <= steps; step++)
            {
                Vector2Int next = start + Vector2Int.RoundToInt((Vector2)delta * (step / (float)steps));
                RemoveTrailCell(next);
                AddTrailCell(lastPosition);
                lastPosition = next;
            }
            RefreshShadows(block, 0);
        }

        void AddTrailCell(Vector2Int cell)
        {
            for (int i = 0; i < trailCount; i++)
            {
                if (trailCells[i] != cell) continue;
                RemoveTrailAt(i);
                break;
            }
            if (trailCount == ShadowCount) trailCount--;
            for (int i = trailCount; i > 0; i--)
            {
                trailCells[i] = trailCells[i - 1];
                trailAges[i] = trailAges[i - 1];
                trailAlphas[i] = trailAlphas[i - 1];
            }
            trailCells[0] = cell;
            trailAges[0] = 0;
            trailAlphas[0] = 0;
            trailCount++;
        }

        void RemoveTrailAt(int index)
        {
            for (int i = index; i < trailCount - 1; i++)
            {
                trailCells[i] = trailCells[i + 1];
                trailAges[i] = trailAges[i + 1];
                trailAlphas[i] = trailAlphas[i + 1];
            }
            trailCount--;
        }

        void RemoveTrailCell(Vector2Int cell)
        {
            for (int i = 0; i < trailCount; i++)
            {
                if (trailCells[i] != cell) continue;
                RemoveTrailAt(i);
                return;
            }
        }

        void Update()
        {
            if (activeBlock == null)
            {
                trailCount = 0;
                HideShadows();
                return;
            }
            bool changed = false;
            for (int i = 0; i < trailCount; i++)
            {
                trailAges[i] += Time.deltaTime;
                if (trailAges[i] < Lifetime) continue;
                RemoveTrailAt(i--);
                changed = true;
            }
            if (trailCount > 0) RefreshShadows(activeBlock, Time.deltaTime);
            else if (changed) HideShadows();
        }

        void RefreshShadows(Block block, float deltaTime)
        {
            Material colorMaterial = block.ColorData != null ? block.ColorData.Material : null;
            Texture texture = colorMaterial != null && colorMaterial.HasProperty(BaseMap)
                ? colorMaterial.GetTexture(BaseMap) : Texture2D.whiteTexture;
            Color baseTint = colorMaterial != null && colorMaterial.HasProperty(BaseColor)
                ? colorMaterial.GetColor(BaseColor) : Color.white;

            for (int i = 0; i < shadows.Length; i++)
            {
                Shadow shadow = shadows[i];
                if (i >= trailCount)
                {
                    shadow.renderer.gameObject.SetActive(false);
                    continue;
                }
                shadow.filter.sharedMesh = meshCache.Get(block.Cells);
                shadow.transform.localPosition = ToBoardPosition(trailCells[i]);
                if (!shadow.renderer.gameObject.activeSelf) shadow.renderer.gameObject.SetActive(true);
                float fadeOut = 1 - Mathf.SmoothStep(0, 1,
                    (trailAges[i] - FadeOutStart) / (Lifetime - FadeOutStart));
                float targetAlpha = trailStrength[i] * fadeOut;
                trailAlphas[i] = Mathf.MoveTowards(trailAlphas[i], targetAlpha,
                    deltaTime / FadeInDuration);
                Color tint = baseTint;
                tint.a *= BaseAlpha * trailAlphas[i];
                properties.Clear();
                properties.SetTexture(BaseMap, texture);
                properties.SetColor(BaseColor, tint);
                shadow.renderer.SetPropertyBlock(properties);
            }
        }

        void HideShadows()
        {
            foreach (Shadow shadow in shadows)
                if (shadow.renderer.gameObject.activeSelf) shadow.renderer.gameObject.SetActive(false);
        }

        void CreateShadow(Shadow shadow, int index)
        {
            var root = new GameObject("FloorShadowTrail_" + index);
            shadow.transform = root.transform;
            shadow.transform.SetParent(board, false);
            shadow.filter = root.AddComponent<MeshFilter>();
            shadow.renderer = root.AddComponent<MeshRenderer>();
            shadow.renderer.sharedMaterial = material;
            shadow.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadow.renderer.receiveShadows = false;
            root.SetActive(false);
        }

        Vector3 ToBoardPosition(Vector2 gridPosition)
        {
            return new Vector3(gridPosition.x - (columns - 1) * .5f,
                -.19f,
                (rows - 1) * .5f - gridPosition.y);
        }

        void OnDestroy()
        {
            foreach (Shadow shadow in shadows)
                if (shadow.transform != null) Destroy(shadow.transform.gameObject);
        }
    }
}
