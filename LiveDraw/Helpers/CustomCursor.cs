using Microsoft.Win32.SafeHandles;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AntFu7.LiveDraw
{
    public class CustomCursor : MarkupExtension, IValueConverter
    {
        public int HotSpotX { get; set; } = 0;
        public int HotSpotY { get; set; } = 0;

        public Cursor DefaultCursor { get; set; } = Cursors.Arrow;

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FrameworkElement frameworkElement)
            {
                var container = new Border
                {
                    Child = frameworkElement,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                };

                container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                container.Arrange(new Rect(container.DesiredSize));

                return FromFrameworkElement(container, HotSpotX, HotSpotY);
            }
            else if (value is System.Drawing.Bitmap bitmap)
            {
                return FromBitmap(bitmap, HotSpotX, HotSpotY);
            }
            else
            {
                return DefaultCursor;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public static Cursor FromFrameworkElement(FrameworkElement visual, int hotSpotX, int hotSpotY)
        {
            using (var temporaryPresentationSource = new HwndSource(new HwndSourceParameters()) { RootVisual = (VisualTreeHelper.GetParent(visual)==null ? visual : null) })
            {
                visual.Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(() => { }));

                int width = (int)visual.ActualWidth;
                int height = (int)visual.ActualHeight;

                // Render to a bitmap
                var bitmapSource = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmapSource.Render(visual);

                using (MemoryStream stream = new MemoryStream())
                {
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                    enc.Save(stream);

                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(stream);

                    return FromBitmap(bitmap, hotSpotX, hotSpotY);
                }
            }
        }

        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        public static Cursor FromBitmap(System.Drawing.Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            IconInfo tmp = new IconInfo();
            GetIconInfo(bmp.GetHicon(), ref tmp);
            tmp.xHotspot = xHotSpot;
            tmp.yHotspot = yHotSpot;
            tmp.fIcon = false;

            IntPtr ptr = CreateIconIndirect(ref tmp);
            SafeFileHandle handle = new SafeFileHandle(ptr, true);
            return CursorInteropHelper.Create(handle);
        }
    }
}
