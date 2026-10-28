using UnityEngine;

namespace BlockedIn.MapTools
{
    public sealed partial class GeneratedBoard
    {
        Vector3[] backdropPositions, backdropScales;
        Quaternion[] backdropRotations;
        Vector3[] wallPositions, wallScales;
        Quaternion[] wallRotations;
        bool resultBackdropActive, resultWallsActive;

        void CacheResultSurfaces()
        {
            CacheTransforms(resultBackdrops, out backdropPositions, out backdropScales, out backdropRotations);
            CacheTransforms(resultWalls, out wallPositions, out wallScales, out wallRotations);
            resultBackdropActive = backdrop != null && backdrop.gameObject.activeSelf;
            resultWallsActive = walls != null && walls.gameObject.activeSelf;
        }

        static void CacheTransforms(Transform[] items, out Vector3[] positions,
            out Vector3[] scales, out Quaternion[] rotations)
        {
            int count = items != null ? items.Length : 0;
            positions = new Vector3[count];
            scales = new Vector3[count];
            rotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                Transform item = items[i];
                if (item == null) continue;
                positions[i] = item.localPosition;
                scales[i] = item.localScale;
                rotations[i] = item.localRotation;
            }
        }

        void SampleResultSurfaces(float elapsed)
        {
            SampleSurfaceSet(resultWalls, wallPositions, wallScales, wallRotations,
                elapsed, .2f, .22f, .13f);
            SampleSurfaceSet(resultBackdrops, backdropPositions, backdropScales, backdropRotations,
                elapsed - .025f, .24f, .27f, .09f);
            if (blockGameplay != null && blockGameplay.CameraController != null)
                blockGameplay.CameraController.SetResultFrameProgress(1 - Smooth(Mathf.Clamp01(elapsed / .27f)));
        }

        void SampleSurfaceSet(Transform[] items, Vector3[] positions, Vector3[] scales,
            Quaternion[] rotations, float elapsed, float duration, float spread, float lift)
        {
            if (items == null) return;
            float denominator = Mathf.Max(1, columns + rows - 2);
            for (int i = 0; i < items.Length; i++)
            {
                Transform item = items[i];
                if (item == null) continue;
                float x = positions[i].x + (columns - 1) * .5f;
                float y = (rows - 1) * .5f - positions[i].z;
                float delay = Mathf.Clamp01((x + y) / denominator) * spread;
                float t = Mathf.Clamp01((elapsed - delay) / duration);
                float ease = Smooth(t);
                item.localPosition = positions[i] + new Vector3(0, lift * Mathf.Sin(t * Mathf.PI), .06f * ease);
                item.localScale = scales[i] * Mathf.Lerp(1, .04f, ease);
                item.localRotation = rotations[i] * Quaternion.Euler(-12 * Mathf.Sin(t * Mathf.PI), 0,
                    ((i & 1) == 0 ? 2 : -2) * Mathf.Sin(t * Mathf.PI));
                if (t >= 1) item.gameObject.SetActive(false);
            }
        }

        void FinishResultSurfaces()
        {
            if (walls != null) walls.gameObject.SetActive(false);
            if (backdrop != null) backdrop.gameObject.SetActive(false);
            if (blockGameplay != null && blockGameplay.CameraController != null)
                blockGameplay.CameraController.SetResultFrameProgress(0);
        }

        void RestoreResultSurfaces()
        {
            RestoreTransformSet(resultBackdrops, backdropPositions, backdropScales, backdropRotations);
            RestoreTransformSet(resultWalls, wallPositions, wallScales, wallRotations);
            if (backdrop != null) backdrop.gameObject.SetActive(resultBackdropActive);
            if (walls != null) walls.gameObject.SetActive(resultWallsActive);
            if (blockGameplay != null && blockGameplay.CameraController != null)
                blockGameplay.CameraController.SetResultFrameProgress(1);
        }

        static void RestoreTransformSet(Transform[] items, Vector3[] positions,
            Vector3[] scales, Quaternion[] rotations)
        {
            if (items == null || positions == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                Transform item = items[i];
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.localPosition = positions[i];
                item.localScale = scales[i];
                item.localRotation = rotations[i];
            }
        }
    }
}
