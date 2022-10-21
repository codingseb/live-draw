using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AntFu7.LiveDraw
{
    internal static class CustomCursor
    {
        public static Cursor ConvertToCursor(FrameworkElement visual, Point hotSpot)
        {
            int width = (int)visual.Width;
            int height = (int)visual.Height;

            // Render to a bitmap
            var bitmapSource = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmapSource.Render(visual);

            // Convert to System.Drawing.Bitmap
            var pixels = new int[width * height];
            bitmapSource.CopyPixels(pixels, width * height, 0);
            var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixels[y * width + x]));
                }
            }

            // Save to .ico format
            var stream = new MemoryStream();
            System.Drawing.Icon.FromHandle(bitmap.GetHicon()).Save(stream);

            // Convert saved file into .cur format
            stream.Seek(2, SeekOrigin.Begin);
            stream.WriteByte(2);
            stream.Seek(10, SeekOrigin.Begin);
            stream.WriteByte((byte)(int)(hotSpot.X * width));
            stream.WriteByte((byte)(int)(hotSpot.Y * height));
            stream.Seek(0, SeekOrigin.Begin);

            // Construct Cursor
            return new Cursor(stream);
        }
    }
}
