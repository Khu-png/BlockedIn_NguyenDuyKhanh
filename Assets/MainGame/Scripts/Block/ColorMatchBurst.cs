using System.Collections;
using UnityEngine;

namespace BlockedIn.Blocks
{
    static class ColorMatchBurst
    {
        const int StarCount = 5;
        const float Duration = .48f;
        static readonly Vector2Int[] joinOffsets =
            { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        static Sprite starSprite;

        public static IEnumerator Play(Block first, Block second, Transform board, int columns, int rows, Camera camera)
        {
            if (first == null || second == null || board == null
                || !TryGetJoin(first, second, board, columns, rows, out Vector3 center)) yield break;

            Vector3 normal = camera != null ? -camera.transform.forward : board.up;
            Vector3 right = camera != null ? camera.transform.right : board.right;
            Vector3 up = camera != null ? camera.transform.up : board.forward;
            center = MoveInFrontOfBlocks(center, normal, first, second);
            Quaternion facing = Quaternion.LookRotation(normal, up);
            var effect = new GameObject("ColorMatchStars");
            effect.transform.SetPositionAndRotation(center, facing);
            SpriteRenderer[] stars = CreateStars(effect.transform);
            Vector3[] velocities = CreateVelocities(right, up);
            float[] sizes = CreateSizes();
            Color tint = GetColor(first);
            float elapsed = 0;

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / Duration);
                float fade = 1 - Mathf.SmoothStep(.28f, 1, progress);
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null) continue;
                    float delay = i * .025f;
                    float age = Mathf.Max(0, elapsed - delay);
                    stars[i].transform.position = center + velocities[i] * age - up * (age * age * .55f);
                    float pulse = 1 + .22f * Mathf.Sin(Mathf.PI * progress);
                    stars[i].transform.localScale = Vector3.one * (sizes[i] * pulse);
                    stars[i].transform.localRotation = Quaternion.Euler(0, 0, age * (i % 2 == 0 ? 150 : -150));
                    Color color = Color.Lerp(tint, Color.white, .65f);
                    color.a = fade;
                    stars[i].color = color;
                }
                yield return null;
            }
            Object.Destroy(effect);
        }

        static SpriteRenderer[] CreateStars(Transform parent)
        {
            var renderers = new SpriteRenderer[StarCount];
            for (int i = 0; i < renderers.Length; i++)
            {
                var star = new GameObject("Star");
                star.transform.SetParent(parent, false);
                SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
                renderer.sprite = GetStarSprite();
                renderer.sortingOrder = 500;
                renderers[i] = renderer;
            }
            return renderers;
        }

        static Vector3[] CreateVelocities(Vector3 right, Vector3 up)
        {
            var velocities = new Vector3[StarCount];
            for (int i = 0; i < velocities.Length; i++)
            {
                float angle = Mathf.Lerp(-48, 48, i / (float)(velocities.Length - 1));
                velocities[i] = (up * Mathf.Cos(angle * Mathf.Deg2Rad)
                    + right * Mathf.Sin(angle * Mathf.Deg2Rad)) * UnityEngine.Random.Range(1.1f, 1.55f);
            }
            return velocities;
        }

        static float[] CreateSizes()
        {
            var sizes = new float[StarCount];
            for (int i = 0; i < sizes.Length; i++)
                sizes[i] = UnityEngine.Random.Range(.34f, .48f);
            return sizes;
        }

        static Vector3 MoveInFrontOfBlocks(Vector3 center, Vector3 normal, Block first, Block second)
        {
            float front = float.NegativeInfinity;
            IncludeFront(first.Renderers, normal, ref front);
            IncludeFront(second.Renderers, normal, ref front);
            if (float.IsNegativeInfinity(front)) return center + normal * .3f;
            return center + normal * (front - Vector3.Dot(center, normal) + .14f);
        }

        static void IncludeFront(Renderer[] renderers, Vector3 normal, ref float front)
        {
            if (renderers == null) return;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                Bounds bounds = renderer.bounds;
                Vector3 extents = bounds.extents;
                float projectedExtent = Mathf.Abs(normal.x) * extents.x
                    + Mathf.Abs(normal.y) * extents.y + Mathf.Abs(normal.z) * extents.z;
                front = Mathf.Max(front, Vector3.Dot(bounds.center, normal) + projectedExtent);
            }
        }

        static bool TryGetJoin(Block first, Block second, Transform board, int columns, int rows,
            out Vector3 center)
        {
            for (int i = 0; i < first.Cells.Length; i++)
                for (int side = 0; side < joinOffsets.Length; side++)
                {
                    Vector2Int adjacent = first.GridPosition + first.Cells[i] + joinOffsets[side];
                    Vector2Int secondCell = adjacent - second.GridPosition;
                    if (!second.ContainsCell(secondCell)
                        || !IsPair(first.EdgeAt(first.Cells[i], (BlockSide)side),
                            second.EdgeAt(secondCell, (BlockSide)((side + 2) % 4)))) continue;
                    Vector2 gridPoint = first.GridPosition + first.Cells[i] + (Vector2)joinOffsets[side] * .5f;
                    center = board.TransformPoint(new Vector3(
                        gridPoint.x - (columns - 1) * .5f, 0,
                        (rows - 1) * .5f - gridPoint.y));
                    return true;
                }
            center = default;
            return false;
        }

        static bool IsPair(EdgeType first, EdgeType second)
        {
            return first == EdgeType.Tab && second == EdgeType.Socket
                || first == EdgeType.Socket && second == EdgeType.Tab;
        }

        static Sprite GetStarSprite()
        {
            if (starSprite != null) return starSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Color Match Star",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = (x + .5f) / size * 2 - 1;
                    float py = (y + .5f) / size * 2 - 1;
                    float angle = Mathf.Atan2(py, px);
                    float radius = Mathf.Sqrt(px * px + py * py);
                    float edge = .66f + .29f * Mathf.Cos(angle * 5);
                    byte alpha = (byte)(Mathf.Clamp01(1 - Mathf.SmoothStep(edge - .06f, edge + .06f, radius)) * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            starSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            starSprite.name = "Color Match Star";
            return starSprite;
        }

        static Color GetColor(Block block)
        {
            Material material = block.ColorData != null ? block.ColorData.Material : null;
            if (material == null) return new Color(1, .88f, .3f);
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            return new Color(1, .88f, .3f);
        }
    }
}
