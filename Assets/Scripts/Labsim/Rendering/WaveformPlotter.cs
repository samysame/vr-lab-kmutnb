using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// CPU plotter drawing into a Texture2D shown by a RawImage (render-pipeline independent).
    /// Coordinates: x and y normalized in [0, 1], origin bottom-left. Out-of-range points are clipped
    /// to the border, like a saturated trace on a real scope.
    /// </summary>
    public class WaveformPlotter
    {
        public readonly int Width;
        public readonly int Height;
        public Texture2D Texture { get; private set; }

        readonly Color32[] pixels;
        Color32 background;

        public WaveformPlotter(int width, int height, Color32 backgroundColor)
        {
            Width = Mathf.Max(16, width);
            Height = Mathf.Max(16, height);
            background = backgroundColor;
            pixels = new Color32[Width * Height];
            Texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Bilinear;
            Texture.wrapMode = TextureWrapMode.Clamp;
            Clear();
            Apply();
        }

        public void Clear()
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;
        }

        /// <summary>Dotted division grid plus a solid central cross with minor ticks.</summary>
        public void DrawGrid(int hDivisions, int vDivisions, Color32 grid, Color32 axis)
        {
            for (int d = 1; d < hDivisions; d++)
            {
                int x = d * (Width - 1) / hDivisions;
                for (int y = 0; y < Height; y += 3) Set(x, y, grid);
            }
            for (int d = 1; d < vDivisions; d++)
            {
                int y = d * (Height - 1) / vDivisions;
                for (int x = 0; x < Width; x += 3) Set(x, y, grid);
            }
            int cx = (Width - 1) / 2;
            int cy = (Height - 1) / 2;
            for (int x = 0; x < Width; x++) Set(x, cy, axis);
            for (int y = 0; y < Height; y++) Set(cx, y, axis);

            // Minor ticks: 5 per division on the central axes.
            int hTicks = hDivisions * 5;
            for (int i = 0; i <= hTicks; i++)
            {
                int x = i * (Width - 1) / hTicks;
                for (int k = -3; k <= 3; k++) Set(x, cy + k, axis);
            }
            int vTicks = vDivisions * 5;
            for (int i = 0; i <= vTicks; i++)
            {
                int y = i * (Height - 1) / vTicks;
                for (int k = -3; k <= 3; k++) Set(cx + k, y, axis);
            }
            // Border
            for (int x = 0; x < Width; x++) { Set(x, 0, axis); Set(x, Height - 1, axis); }
            for (int y = 0; y < Height; y++) { Set(0, y, axis); Set(Width - 1, y, axis); }
        }

        /// <summary>Draws a polyline of 'count' samples evenly spread over the width.</summary>
        public void DrawTrace(float[] yNormalized, int count, Color32 color, int thickness)
        {
            if (yNormalized == null || count < 2) return;
            int prevX = 0;
            int prevY = ToPixelY(yNormalized[0]);
            for (int i = 1; i < count; i++)
            {
                int x = (int)((long)i * (Width - 1) / (count - 1));
                int y = ToPixelY(yNormalized[i]);
                DrawLine(prevX, prevY, x, y, color, thickness);
                prevX = x;
                prevY = y;
            }
        }

        public void DrawHorizontalLine(float yNorm, Color32 color, bool dashed)
        {
            int y = ToPixelY(yNorm);
            for (int x = 0; x < Width; x++)
                if (!dashed || (x / 6) % 2 == 0) Set(x, y, color);
        }

        public void DrawVerticalLine(float xNorm, Color32 color, bool dashed)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(xNorm * (Width - 1)), 0, Width - 1);
            for (int y = 0; y < Height; y++)
                if (!dashed || (y / 6) % 2 == 0) Set(x, y, color);
        }

        /// <summary>Small triangle on the left border pointing right (channel ground marker).</summary>
        public void DrawLeftMarker(float yNorm, Color32 color)
        {
            int y0 = ToPixelY(yNorm);
            for (int dx = 0; dx < 10; dx++)
                for (int dy = -(9 - dx) / 2; dy <= (9 - dx) / 2; dy++) Set(dx, y0 + dy, color);
        }

        /// <summary>Small triangle on the right border pointing left (trigger level marker).</summary>
        public void DrawRightMarker(float yNorm, Color32 color)
        {
            int y0 = ToPixelY(yNorm);
            for (int dx = 0; dx < 10; dx++)
                for (int dy = -(9 - dx) / 2; dy <= (9 - dx) / 2; dy++) Set(Width - 1 - dx, y0 + dy, color);
        }

        /// <summary>Small triangle on the top border pointing down (trigger position marker).</summary>
        public void DrawTopMarker(float xNorm, Color32 color)
        {
            int x0 = Mathf.RoundToInt(Mathf.Clamp01(xNorm) * (Width - 1));
            for (int dy = 0; dy < 10; dy++)
                for (int dx = -(9 - dy) / 2; dx <= (9 - dy) / 2; dx++) Set(x0 + dx, Height - 1 - dy, color);
        }

        public void DrawLine(int x0, int y0, int x1, int y1, Color32 c, int thickness)
        {
            // Bresenham
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Plot(x0, y0, c, thickness);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void Apply()
        {
            Texture.SetPixels32(pixels);
            Texture.Apply(false);
        }

        public void Dispose()
        {
            if (Texture != null) Object.Destroy(Texture);
            Texture = null;
        }

        int ToPixelY(float yNorm)
        {
            return Mathf.Clamp(Mathf.RoundToInt(yNorm * (Height - 1)), 0, Height - 1);
        }

        void Plot(int x, int y, Color32 c, int thickness)
        {
            int half = thickness / 2;
            for (int ox = -half; ox < thickness - half; ox++)
                for (int oy = -half; oy < thickness - half; oy++)
                    Set(x + ox, y + oy, c);
        }

        void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            pixels[y * Width + x] = c;
        }
    }
}
