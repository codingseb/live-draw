using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AntFu7.LiveDraw
{
    public class CustomCursor : MarkupExtension, IValueConverter
    {
        public Point HotSpot { get; set; } = new Point(0, 0);
        public Cursor DefaultCursor { get; set; } = Cursors.Arrow;

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is FrameworkElement frameworkElement)
            {
                frameworkElement.Measure(new Size(frameworkElement.Width, frameworkElement.Height));
                frameworkElement.Arrange(new Rect(new Size(frameworkElement.Width, frameworkElement.Height)));

                return FromFrameworkElement(frameworkElement, HotSpot);
            }
            else if(value is System.Drawing.Bitmap bitmap)
            {
                return FromBitmap(bitmap, HotSpot);
            }
            else
            {
                return DefaultCursor;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public static Cursor FromFrameworkElement(FrameworkElement visual, Point hotSpot)
        {
            int width = (int)visual.Width;
            int height = (int)visual.Height;

            // Render to a bitmap
            var bitmapSource = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmapSource.Render(visual);

            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                enc.Save(stream);

                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(stream);

                return FromBitmap(bitmap, hotSpot);
            }
        }

        public static Cursor FromBitmap(System.Drawing.Bitmap bitmap, Point hotSpot)
        {
            using (var stream = new MemoryStream())
            {
                System.Drawing.Icon.FromHandle(bitmap.GetHicon()).Save(stream);

                // Convert saved file into .cur format
                stream.Seek(2, SeekOrigin.Begin);
                stream.WriteByte(2);
                stream.Seek(10, SeekOrigin.Begin);
                stream.WriteByte((byte)(int)(hotSpot.X * bitmap.Width));
                stream.WriteByte((byte)(int)(hotSpot.Y * bitmap.Height));
                stream.Seek(0, SeekOrigin.Begin);

                return new Cursor(stream);
            }
        }
    }
}
