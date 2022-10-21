using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace AntFu7.LiveDraw
{
    public class TextToBrushConverter : MarkupExtension, IValueConverter
    {
        public Brush DefaultBrush { get; set; } = Brushes.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is string text)
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(text));
                }
                catch { }
            }

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            else
            {
                return Colors.White.ToString();
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
