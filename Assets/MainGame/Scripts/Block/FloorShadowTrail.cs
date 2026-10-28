using UnityEngine;

namespace BlockedIn.Blocks
{
    sealed class FloorShadowTrail : MonoBehaviour
    {
        const int ShadowCount = 6;
        const float CellCrossingThreshold = .58f;
        const float FadeInDuration = .025f;
        const float Lifetime = .28f;
        const float ReleaseFade = .09f;
        const float BaseAlpha = .38f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        sealed class Shadow
        {
            public Transform transform;
            public MeshRenderer renderer;
            public Vector2Int cell;
            public float age;
            public bool active;
        }

        readonly Shadow[] shadows = new Shadow[ShadowCount];
        Transform board;
        int columns, rows;
        PlacementPreviewMeshCache meshCache;
        Material material;
        MaterialPropertyBlock properties;
        Block activeBlock;
        Vector2Int lastPosition;
        Color tint;

        public void Initialize(Transform boardTransform, int boardColumns, int boardRows, Material shadowMaterial,
            PlacementPreviewMeshCache sharedMeshCache)
        {
            board = boardTransform;
            columns = boardColumns;
            rows = boardRows;
            material = shadowMaterial;
            meshCache = sharedMeshCache;
            properties = new MaterialPropertyBlock();
            for (int i = 0; i < shadows.Length; i++) shadows[i] = CreateShadow(i);
        }

        public void Begin(Block block, Vector2 gridPosition)
        {
            activeBlock = block;
            lastPosition = Vector2Int.RoundToInt(gridPosition);
            Material source = block.ColorData != null ? block.ColorData.Material : null;
            Texture texture = source != null && source.HasProperty(BaseMap) ? source.GetTexture(BaseMap) : null;
            tint = source != null && source.HasProperty(BaseColor) ? source.GetColor(BaseColor) : Color.white;
            properties.SetTexture(BaseMap, texture != null ? texture : Texture2D.whiteTexture);
            Mesh mesh = meshCache.Get(block.Cells);
            foreach (Shadow shadow in shadows)
            {
                Hide(shadow);
                // The footprint and texture stay fixed throughout this drag.
                shadow.renderer.SetPropertyBlock(properties);
            }
            foreach (MeshFilter filter in filters) filter.sharedMesh = mesh;
        }

        public void Capture(Block block, Vector2 gridPosition)
        {
            if (block == null || block != activeBlock || board == null || material == null || meshCache == null) return;
            Vector2Int destination = Vector2Int.RoundToInt(gridPosition);
            Vector2Int delta = destination - lastPosition;
            if (delta.x != 0 && Mathf.Abs(gridPosition.x - lastPosition.x) < CellCrossingThreshold) delta.x = 0;
            if (delta.y != 0 && Mathf.Abs(gridPosition.y - lastPosition.y) < CellCrossingThreshold) delta.y = 0;
            int steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            Vector2Int start = lastPosition;
            for (int step = 1; step <= steps; step++)
            {
                Vector2Int next = start + Vector2Int.RoundToInt((Vector2)delta * (step / (float)steps));
                foreach (Shadow shadow in shadows)
                    if (shadow.active && shadow.cell == next) shadow.age = Mathf.Max(shadow.age, Lifetime - ReleaseFade);
                AddTrailCell(lastPosition);
                lastPosition = next;
            }
        }

        void AddTrailCell(Vector2Int cell)
        {
            Shadow available = null, oldest = shadows[0];
            foreach (Shadow shadow in shadows)
            {
                if (shadow.active && shadow.cell == cell) return;
                if (!shadow.active && available == null) available = shadow;
                if (shadow.age > oldest.age) oldest = shadow;
            }
            Shadow target = available ?? oldest;
            target.cell = cell;
            target.age = 0;
            target.active = true;
            target.transform.localPosition = new Vector3(cell.x - (columns - 1) * .5f,
                -.205f, (rows - 1) * .5f - cell.y);
            ApplyAlpha(target, 0);
            target.renderer.gameObject.SetActive(true);
        }

        void Update() => Tick(Time.deltaTime);

        void Tick(float deltaTime)
        {
            foreach (Shadow shadow in shadows)
            {
                if (shadow == null || !shadow.active) continue;
                if (activeBlock == null) { Hide(shadow); continue; }
                if (!activeBlock.IsDragging) shadow.age = Mathf.Max(shadow.age, Lifetime - ReleaseFade);
                shadow.age += deltaTime;
                if (shadow.age >= Lifetime) { Hide(shadow); continue; }
                float appear = Mathf.SmoothStep(0, 1, shadow.age / FadeInDuration);
                float fade = 1 - Mathf.SmoothStep(0, 1, (shadow.age - FadeInDuration) / (Lifetime - FadeInDuration));
                ApplyAlpha(shadow, appear * fade);
            }
        }

        void ApplyAlpha(Shadow shadow, float alpha)
        {
            Color color = tint;
            color.a *= BaseAlpha * alpha;
            properties.SetColor(BaseColor, color);
            shadow.renderer.SetPropertyBlock(properties);
        }

        static void Hide(Shadow shadow)
        {
            shadow.active = false;
            shadow.renderer.gameObject.SetActive(false);
        }

        readonly MeshFilter[] filters = new MeshFilter[ShadowCount];
        Shadow CreateShadow(int index)
        {
            var root = new GameObject("FloorShadowTrail_" + index);
            root.transform.SetParent(board, false);
            filters[index] = root.AddComponent<MeshFilter>();
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            root.SetActive(false);
            return new Shadow { transform = root.transform, renderer = renderer };
        }

        void OnDestroy()
        {
            foreach (Shadow shadow in shadows)
                if (shadow != null && shadow.transform != null) Destroy(shadow.transform.gameObject);
        }
    }
}
