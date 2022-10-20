using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace AntFu7.LiveDraw
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Point
    {
        public Int32 X;
        public Int32 Y;
    };

    internal static class Win32
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        static public Color GetPixelColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromRgb((byte)(pixel & 0x000000FF),
                (byte)(((int)pixel & 0x0000FF00) >> 8),
                (byte)(((int)pixel & 0x00FF0000) >> 16));
            return color;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
    }
}
