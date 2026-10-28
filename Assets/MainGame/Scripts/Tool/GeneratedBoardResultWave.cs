using System.Collections;
using System.Collections.Generic;
using BlockedIn.Blocks;
using UnityEngine;

namespace BlockedIn.MapTools
{
    public sealed partial class GeneratedBoard
    {
        const float ObstacleDuration = .15f;
        const float ObstacleStagger = .009f;
        const float CellDuration = .29f;
        const float WaveSpread = .29f;
        readonly List<Block> resultObstacles = new List<Block>();
        Vector3[] obstacleScales;
        Vector3[] cellPositions, cellScales;
        Quaternion[] cellRotations;
        bool resultCached, resultPlaying;

        public IEnumerator PlayWinResult()
        {
            CacheResultState();
            resultPlaying = true;
            float obstacleTime = ObstacleDuration
                + Mathf.Max(0, resultObstacles.Count - 1) * ObstacleStagger;
            float elapsed = 0;
            while (elapsed < obstacleTime)
            {
                elapsed += Time.unscaledDeltaTime;
                SampleObstacles(elapsed);
                yield return null;
            }
            elapsed = 0;
            float waveTime = CellDuration + WaveSpread;
            while (elapsed < waveTime)
            {
                elapsed += Time.unscaledDeltaTime;
                SampleCells(elapsed);
                yield return null;
            }
            FinishResultState();
        }

        void CacheResultState()
        {
            if (resultCached) { RestoreWinResult(); return; }
            resultCached = true;
            resultObstacles.Clear();
            if (blockGameplay != null)
                foreach (Block block in blockGameplay.Blocks)
                    if (block != null && block.ColorData != null && block.ColorData.IsObstacle)
                        resultObstacles.Add(block);
            obstacleScales = new Vector3[resultObstacles.Count];
            for (int i = 0; i < resultObstacles.Count; i++)
                obstacleScales[i] = resultObstacles[i].transform.localScale;
            int count = resultCells != null ? resultCells.Length : 0;
            cellPositions = new Vector3[count];
            cellScales = new Vector3[count];
            cellRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                Transform cell = resultCells[i];
                if (cell == null) continue;
                cellPositions[i] = cell.localPosition;
                cellScales[i] = cell.localScale;
                cellRotations[i] = cell.localRotation;
            }
            CacheResultSurfaces();
        }

        void SampleObstacles(float elapsed)
        {
            for (int i = 0; i < resultObstacles.Count; i++)
            {
                Block block = resultObstacles[i];
                if (block == null) continue;
                float t = Mathf.Clamp01((elapsed - i * ObstacleStagger) / ObstacleDuration);
                float ease = t * t * (3 - 2 * t);
                float pop = Mathf.Sin(t * Mathf.PI) * .08f;
                block.transform.localScale = obstacleScales[i] * Mathf.Max(0, 1 - ease + pop);
                if (t >= 1) block.gameObject.SetActive(false);
            }
        }

        void SampleCells(float elapsed)
        {
            int floorIndex = 0;
            float denominator = Mathf.Max(1, columns + rows - 2);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int mapIndex = y * columns + x;
                    if (cells == null || mapIndex >= cells.Length || cells[mapIndex] != BoardCell.Floor) continue;
                    if (floorIndex < resultCells.Length)
                        SampleCell(floorIndex, elapsed - (x + y) / denominator * WaveSpread);
                    floorIndex++;
                }
            SampleResultSurfaces(elapsed);
        }

        void SampleCell(int index, float localTime)
        {
            Transform cell = resultCells[index];
            if (cell == null) return;
            float t = Mathf.Clamp01(localTime / CellDuration);
            float ease = Smooth(t);
            cell.localPosition = cellPositions[index] + new Vector3(0, .22f * Mathf.Sin(t * Mathf.PI), .12f * ease);
            cell.localScale = cellScales[index] * Mathf.Lerp(1, .04f, ease);
            cell.localRotation = cellRotations[index] * Quaternion.Euler(-18 * Mathf.Sin(t * Mathf.PI), 0,
                ((index & 1) == 0 ? 3 : -3) * Mathf.Sin(t * Mathf.PI));
            if (t >= 1) cell.gameObject.SetActive(false);
        }

        static float Smooth(float value) => value * value * (3 - 2 * value);

        void FinishResultState()
        {
            resultPlaying = false;
            FinishResultSurfaces();
        }

        public void RestoreWinResult()
        {
            if (!resultCached)
            {
                if (blockGameplay != null && blockGameplay.CameraController != null)
                    blockGameplay.CameraController.SetResultFrameProgress(1);
                return;
            }
            RestoreResultSurfaces();
            resultPlaying = false;
            for (int i = 0; i < resultObstacles.Count; i++)
            {
                Block block = resultObstacles[i];
                if (block == null) continue;
                block.gameObject.SetActive(true);
                block.transform.localScale = obstacleScales[i];
            }
            for (int i = 0; i < resultCells.Length; i++)
            {
                Transform cell = resultCells[i];
                if (cell == null) continue;
                cell.gameObject.SetActive(true);
                cell.localPosition = cellPositions[i];
                cell.localScale = cellScales[i];
                cell.localRotation = cellRotations[i];
            }
        }

        void OnDisable()
        {
            if (resultPlaying) RestoreWinResult();
        }
    }
}
