using BlockedIn.MapTools;
using UnityEngine;
namespace BlockedIn.CameraTools
{
    public static class BoardSurfaceShape
    {
        const int PixelsPerCell = 20;
        public static Texture2D CreateCells(GeneratedBoard board)
        {
            var texture = new Texture2D(board.columns, board.rows, TextureFormat.RGBA32, false, true);
            texture.name = "BoardFloorMask";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[board.columns * board.rows];
            for (int y = 0; y < board.rows; y++)
                for (int x = 0; x < board.columns; x++)
                    pixels[y * board.columns + x] = IsFloor(board, x, y) ? new Color32(255,255,255,255) : new Color32(0,0,0,255);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
        public static Texture2D CreateOutline(GeneratedBoard board)
        {
            int width = (board.columns + 2) * PixelsPerCell;
            int height = (board.rows + 2) * PixelsPerCell;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.name = "BoardOutlineDistance";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2((x + .5f) / PixelsPerCell - 1, (y + .5f) / PixelsPerCell - 1);
                    float distance = Distance(board, point);
                    byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01(distance * .5f + .5f) * 255);
                    pixels[y * width + x] = new Color32(value, value, value, 255);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
        static bool IsFloor(GeneratedBoard board, int x, int y)
        {
            if (x < 0 || y < 0 || x >= board.columns || y >= board.rows) return false;
            int index = y * board.columns + x;
            return board.cells != null && index < board.cells.Length && board.cells[index] == BoardCell.Floor;
        }
        static float Distance(GeneratedBoard board, Vector2 point)
        {
            int column = Mathf.FloorToInt(point.x), row = Mathf.FloorToInt(point.y);
            float distance = 1;
            for (int y = row - 1; y <= row + 1; y++)
                for (int x = column - 1; x <= column + 1; x++)
                {
                    if (!IsFloor(board, x, y)) continue;
                    Vector2 offset = point - new Vector2(x + .5f, y + .5f);
                    Vector2 q = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) - Vector2.one * .49f;
                    float roundedBox = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - .20f;
                    distance = Mathf.Min(distance, roundedBox);
                }
            return distance;
        }
    }
}
