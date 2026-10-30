using System;
using System.Collections;
using System.Collections.Generic;
using BlockedIn.Blocks;
using UnityEngine;

public sealed class WinTransitionEffect : MonoBehaviour
{
    [SerializeField] Block blockPrefab;
    [SerializeField] Camera transitionCamera;
    [SerializeField] Transform overlayParent;
    [SerializeField] BlockColorData[] colors = Array.Empty<BlockColorData>();
    [SerializeField, Min(.1f)] float cellSpacing = 1f;
    [SerializeField, Min(.1f)] float blockScale = 1.12f;
    readonly List<FallingPiece> pieces = new List<FallingPiece>();
    Coroutine routine;
    WinTransitionOverlay overlay;

    public void Play(Action onCovered)
    {
        if (routine != null) return;
        Validate(onCovered);
        routine = StartCoroutine(FallAndCover(onCovered));
    }

    void Validate(Action onCovered)
    {
        if (onCovered == null) throw new ArgumentNullException(nameof(onCovered));
        if (blockPrefab == null || transitionCamera == null || overlayParent == null
            || colors == null || colors.Length == 0)
            throw new InvalidOperationException("Assign the transition block, camera, overlay parent and color palette.");
        if (transitionCamera.pixelRect.width <= 0f || transitionCamera.pixelRect.height <= 0f)
            throw new InvalidOperationException("The transition camera must have a valid display area.");
        foreach (BlockColorData color in colors)
            if (color == null || color.Material == null)
                throw new InvalidOperationException("Every transition color must have a material.");
        if (UIManager.Ins == null)
            throw new InvalidOperationException("The UI manager is required for the win transition.");
    }

    IEnumerator FallAndCover(Action onCovered)
    {
        CreatePieces();
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, .034f);
            bool allSettled = true;
            foreach (FallingPiece piece in pieces)
            {
                UpdatePiece(piece, elapsed, deltaTime);
                if (!piece.Settled) allSettled = false;
            }
            overlay.Render();
            if (allSettled) break;
            yield return null;
        }

        try
        {
            UIManager.Ins.CloseAll();
            onCovered();
        }
        finally
        {
            ClearPieces();
            routine = null;
        }
    }

    void CreatePieces()
    {
        pieces.Clear();
        if (overlay == null) overlay = new WinTransitionOverlay(transitionCamera, overlayParent);
        overlay.Begin();
        Rect pixelRect = transitionCamera.pixelRect;
        float depth = transitionCamera.nearClipPlane + 2f;
        Vector3 screenCenter = transitionCamera.ScreenToWorldPoint(
            new Vector3(pixelRect.center.x, pixelRect.center.y, depth));
        float worldWidth = WorldDistance(
            new Vector3(pixelRect.xMin, pixelRect.center.y, depth),
            new Vector3(pixelRect.xMax, pixelRect.center.y, depth));
        float worldHeight = WorldDistance(
            new Vector3(pixelRect.center.x, pixelRect.yMin, depth),
            new Vector3(pixelRect.center.x, pixelRect.yMax, depth));
        int columns = Mathf.CeilToInt(worldWidth / cellSpacing) + 1;
        int rows = Mathf.CeilToInt(worldHeight / cellSpacing) + 1;
        Quaternion blockRotation = Quaternion.LookRotation(
            transitionCamera.transform.up, -transitionCamera.transform.forward);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector3 target = screenCenter
                    + transitionCamera.transform.right * ((column - (columns - 1) * .5f) * cellSpacing)
                    + transitionCamera.transform.up * ((row - (rows - 1) * .5f) * cellSpacing);
                float startHeight = worldHeight + blockScale;
                Block block = Instantiate(blockPrefab, target + transitionCamera.transform.up
                    * startHeight, blockRotation);
                block.transform.localScale = Vector3.one * blockScale;
                block.SetColor(colors[(column + row * 3) % colors.Length]);
                block.SetTransitionLayer(WinTransitionOverlay.BlockLayer);
                block.enabled = false;
                if (block.HitCollider != null) block.HitCollider.enabled = false;
                pieces.Add(new FallingPiece(block, target, startHeight,
                    row * .012f + UnityEngine.Random.Range(0f, .07f)));
            }
        }
    }

    float WorldDistance(Vector3 screenStart, Vector3 screenEnd)
    {
        Vector3 start = transitionCamera.ScreenToWorldPoint(screenStart);
        Vector3 end = transitionCamera.ScreenToWorldPoint(screenEnd);
        return Vector3.Distance(start, end);
    }

    void UpdatePiece(FallingPiece piece, float elapsed, float deltaTime)
    {
        if (piece.Settled || elapsed < piece.Delay) return;
        piece.FallSpeed += 36f * deltaTime;
        piece.Height -= piece.FallSpeed * deltaTime;
        piece.SideOffset += piece.SideSpeed * deltaTime;
        piece.SideSpeed -= piece.SideOffset * 5f * deltaTime;
        piece.SideSpeed *= Mathf.Exp(-4f * deltaTime);
        piece.Angle += piece.AngularSpeed * deltaTime;
        piece.AngularSpeed -= piece.Angle * 9f * deltaTime;
        piece.AngularSpeed *= Mathf.Exp(-4f * deltaTime);

        if (piece.Height <= 0f)
        {
            piece.Height = 0f;
            if (!piece.Bounced && piece.FallSpeed > 3f)
            {
                piece.FallSpeed *= -.12f;
                piece.Bounced = true;
            }
            else
            {
                piece.Settled = true;
                piece.SideOffset = 0f;
                piece.Angle = 0f;
                piece.AngularSpeed = 0f;
            }
        }

        piece.Block.transform.position = piece.Target
            + transitionCamera.transform.up * piece.Height
            + transitionCamera.transform.right * piece.SideOffset;
        piece.Block.transform.rotation = piece.Rotation
            * Quaternion.AngleAxis(piece.Angle, Vector3.up);
    }

    void ClearPieces()
    {
        foreach (FallingPiece piece in pieces)
            if (piece.Block != null) Destroy(piece.Block.gameObject);
        pieces.Clear();
        overlay?.End();
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        ClearPieces();
    }

    sealed class FallingPiece
    {
        public readonly Block Block;
        public readonly Vector3 Target;
        public readonly Quaternion Rotation;
        public readonly float Delay;
        public float Height;
        public float FallSpeed;
        public float SideOffset;
        public float SideSpeed;
        public float Angle;
        public float AngularSpeed;
        public bool Bounced;
        public bool Settled;

        public FallingPiece(Block block, Vector3 target, float height, float delay)
        {
            Block = block;
            Target = target;
            Rotation = block.transform.rotation;
            Delay = delay;
            Height = height;
            SideSpeed = UnityEngine.Random.Range(-.15f, .15f);
            Angle = UnityEngine.Random.Range(-8f, 8f);
            AngularSpeed = UnityEngine.Random.Range(-35f, 35f);
        }
    }
}
